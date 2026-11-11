using System.Text;
using Jason.API.Options;
using Jason.API.Parsers;
using Jason.DataAccess.Repositories;
using Jason.Models.Entities;
using Jason.Models.Exchange;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jason.API.Services;

public interface IFeedService
{
    Task<DashboardExport> GetAsync(CancellationToken cancellationToken = default);
}

public class FeedService : IFeedService
{
    private readonly ISourceRepository _sourceRepository;
    private readonly IContentParserFactory _parserFactory;
    private readonly ISecretService _secretService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly AppOptions _appOptions;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        ISourceRepository sourceRepository,
        IContentParserFactory parserFactory,
        ISecretService secretService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<AppOptions> appOptions,
        ILogger<FeedService> logger)
    {
        _sourceRepository = sourceRepository;
        _parserFactory = parserFactory;
        _secretService = secretService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _appOptions = appOptions.Value;
        _logger = logger;
    }


    // Este método obtiene los datos de todas las fuentes y los devuelve como un objeto DashboardExport
    public async Task<DashboardExport> GetAsync(CancellationToken cancellationToken = default //aqui le asignamos un valor por defecto al cancellationToken
    )
    {
        // Creamos un nuevo objeto DashboardExport y le asignamos la fecha y hora actual en formato UTC
        DashboardExport dashboard = new();
        dashboard.ExportedAt = DateTime.UtcNow;

        IEnumerable<Source> sources = await _sourceRepository.ReadAsync();

        foreach (Source source in sources)//aqui hacemos un loop para recorrer todas las fuentes
        {
            ExportSource exportSource = CreateExportSource(source); //le definimos una variable exportSource y le asignamos el resultado de 

            try //aqui hacemos un try catch para intentar descargar la fuente y si falla, se registra el problema y se continúa con las demás
                //esto permite que no se detenga todo un proceso por una sola fuente verdad
            {
                CachedSource cachedSource = await DownloadSource(source, exportSource, cancellationToken);
                if (!string.IsNullOrWhiteSpace(cachedSource.Format))
                    exportSource.Format = cachedSource.Format;
                exportSource.Items = cachedSource.Items.ToList();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Si una fuente falla, se registra el problema y se continúa con las demás.
                _logger.LogWarning(exception,
                    "No se pudo cargar la fuente {SourceId} ({SourceName})", source.Id, source.Name);

                // También recuerda el fallo por un minuto para no insistir inmediatamente
                // contra una API que está caída.
                _cache.Set(CreateCacheKey(source), new CachedSource(), TimeSpan.FromMinutes(1));
            }

            dashboard.Sources.Add(exportSource);
        }

        return dashboard;
    }

    private async Task<CachedSource> DownloadSource(
        Source source,
        ExportSource exportSource,
        CancellationToken cancellationToken)
    {
        string cacheKey = CreateCacheKey(source);

        CachedSource? savedInCache;
        if (_cache.TryGetValue(cacheKey, out savedInCache) && savedInCache != null)
            return savedInCache;

        Uri sourceUrl = ValidateUrl(source.Url);
        using HttpRequestMessage request = new(HttpMethod.Get, sourceUrl);
        request.Headers.UserAgent.ParseAdd("Jason/1.0");
        request.Headers.Accept.ParseAdd(
            "application/json, application/xml, text/xml, text/html;q=0.9, */*;q=0.5");

        if (source.RequiresSecret)
            await AddSecret(request, source);

        HttpClient client = _httpClientFactory.CreateClient("Feed");
        using HttpResponseMessage response = await client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("La fuente " + source.Name + " devolvió una respuesta vacía.");

        string? contentType = response.Content.Headers.ContentType?.MediaType;
        string format = _parserFactory.DetectFormat(content, contentType);
        IContentParser parser = _parserFactory.Create(format);
        IReadOnlyList<ExportItem> items = parser.Parse(content, exportSource);

        CachedSource result = new();
        result.Format = format;
        result.Items = items;

        int cacheMinutes = Math.Max(1, _appOptions.FeedCacheMinutes);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(cacheMinutes));

        return result;
    }


    //Este metodo es uno de los mas importantes, basicamente es el que agrega el secret a la fuente,
    //si la fuente requiere un secret, este metodo lo agrega al request
    private async Task AddSecret(HttpRequestMessage request, Source source)
    {
        SourceSecretValue? secret = await _secretService.GetForSourceAsync(source.Id);
        if (secret == null)
        {
            throw new InvalidOperationException(
                "La fuente " + source.Name + " requiere un secret asociado en Configuración.");
        }

        if (secret.Key.StartsWith("header:", StringComparison.OrdinalIgnoreCase))
        {
            string headerName = secret.Key.Substring("header:".Length).Trim();
            if (string.IsNullOrWhiteSpace(headerName)
                || !request.Headers.TryAddWithoutValidation(headerName, secret.Value))
            {
                throw new InvalidOperationException("El nombre del header del secret no es válido.");
            }
            return;
        }

        string parameterName = "api_key";
        if (secret.Key.StartsWith("query:", StringComparison.OrdinalIgnoreCase))
            parameterName = secret.Key.Substring("query:".Length).Trim();

        if (string.IsNullOrWhiteSpace(parameterName))
            throw new InvalidOperationException("El nombre del parámetro del secret no es válido.");

        string urlWithSecret = QueryHelpers.AddQueryString(
            request.RequestUri!.ToString(), parameterName, secret.Value);
        request.RequestUri = new Uri(urlWithSecret);
    }

    private static Uri ValidateUrl(string url)
    {
        Uri? result;
        if (!Uri.TryCreate(url, UriKind.Absolute, out result)
            || (result.Scheme != "http" && result.Scheme != "https"))
        {
            throw new InvalidOperationException("La URL de la fuente debe usar HTTP o HTTPS.");
        }

        return result;
    }

    private static string CreateCacheKey(Source source)
    {
        return "feed:" + source.Id + ":" + source.Url;
    }

    private static ExportSource CreateExportSource(Source source)
    {
        ExportSource exportSource = new();
        exportSource.Id = CreateSlug(source.Name, source.Id);
        exportSource.Name = source.Name;
        exportSource.Url = source.Url;
        exportSource.Description = source.Description;
        exportSource.ComponentType = source.ComponentType;
        exportSource.RequiresSecret = source.RequiresSecret;
        exportSource.Category = ContentParserUtilities.NormalizeCategory(null, exportSource);
        return exportSource;
    }

    private static string CreateSlug(string name, int id)
    {
        StringBuilder result = new();

        foreach (char character in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) && character <= 127)
            {
                result.Append(character);
            }
            else if ((char.IsWhiteSpace(character) || character == '-' || character == '_')
                     && result.Length > 0 && result[result.Length - 1] != '-')
            {
                result.Append('-');
            }
        }

        string slug = result.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "source";

        return slug + "-" + id;
    }

    // Clase sencilla para guardar en caché el formato y las fichas ya procesadas.
    private class CachedSource
    {
        public string Format { get; set; } = string.Empty;
        public IReadOnlyList<ExportItem> Items { get; set; } = Array.Empty<ExportItem>();
    }
}