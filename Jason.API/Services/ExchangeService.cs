using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Jason.API.Exceptions;
using Jason.DataAccess.Repositories;
using Jason.Models.Dtos;
using Jason.Models.Entities;
using Jason.Models.Exchange;
using Json.Schema;

namespace Jason.API.Services;

public interface IExchangeService
{
    // Arma el archivo JSON (formato común) de un item guardado
    Task<ExportFileDto> ExportItemAsync(int itemId);

    // Arma un solo archivo con todas las fichas guardadas, agrupadas por fuente
    Task<ExportFileDto> ExportAllAsync();

    // Valida un archivo JSON de cualquier grupo y guarda sus fuentes e items.
    // Con preview = true solo calcula qué pasaría, sin guardar nada.
    Task<ImportResultDto> ImportAsync(IFormFile? file, bool preview);
}

public class ExchangeService : IExchangeService
{
    private const string GroupName = "Grupo 3";
    private const long MaxFileBytes = 5 * 1024 * 1024;

    // El schema registra su $id y no se puede cargar dos veces, por eso se lee una sola vez y se reutiliza
    private static readonly Lazy<JsonSchema> Schema = new(() =>
        JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", "schema.json"))));

    // Para guardar el item en la columna Json no hace falta que vaya con sangría
    private static readonly JsonSerializerOptions CompactOptions = new(DashboardExport.JsonOptions) { WriteIndented = false };

    // Parámetros de URL que suelen llevar una API key: nunca deben salir en un archivo para compartir
    private static readonly Regex SecretParams = new(
        @"(?<=[?&])(api[_-]?key|key|token|access_token|client_secret|secret|password|auth)=[^&#]*&?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ISourceRepository _sources;
    private readonly ISourceItemRepository _items;

    public ExchangeService(ISourceRepository sources, ISourceItemRepository items)
    {
        _sources = sources;
        _items = items;
    }

    public async Task<ExportFileDto> ExportItemAsync(int itemId)
    {
        var entity = await _items.FindAsync(itemId)
            ?? throw ApiException.NotFound("No existe la ficha que quieres descargar.");
        var source = await _sources.FindAsync(entity.SourceId)
            ?? throw ApiException.NotFound("La fuente de esa ficha ya no existe.");

        var item = ParseItem(entity.Json)
            ?? throw ApiException.Conflict("La ficha guardada no tiene el formato común, por eso no se puede descargar.");

        var sourceSlug = Slugify(source.Name);
        var json = BuildFile(new List<ExportSource> { BuildSource(source, sourceSlug, new List<ExportItem> { item }) });

        return new ExportFileDto
        {
            FileName = $"{sourceSlug}-{Slugify(item.Id)}.json",
            Json = json
        };
    }

    public async Task<ExportFileDto> ExportAllAsync()
    {
        var sources = (await _sources.ReadAsync()).OrderBy(s => s.Name).ToList();
        var entities = (await _items.ReadAsync()).OrderBy(i => i.CreatedAt).ToList();

        var exportSources = new List<ExportSource>();
        var usedIds = new HashSet<string>();

        foreach (var source in sources)
        {
            // Las fichas que no tienen el formato común se saltan para no dañar el archivo completo
            var items = entities
                .Where(i => i.SourceId == source.Id)
                .Select(i => ParseItem(i.Json))
                .OfType<ExportItem>()
                .ToList();
            if (items.Count == 0)
                continue;

            // El id de cada fuente no se puede repetir dentro del archivo
            var slug = Slugify(source.Name);
            var candidate = slug;
            for (var n = 2; !usedIds.Add(candidate); n++)
                candidate = $"{slug}-{n}";

            exportSources.Add(BuildSource(source, candidate, items));
        }

        if (exportSources.Count == 0)
            throw ApiException.NotFound("Todavía no hay fichas guardadas para descargar.");

        return new ExportFileDto
        {
            FileName = $"jason-fichas-{DateTime.UtcNow:yyyyMMdd}.json",
            Json = BuildFile(exportSources)
        };
    }

    // null si el texto guardado no es un item del formato común
    private static ExportItem? ParseItem(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ExportItem>(json, DashboardExport.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ExportSource BuildSource(Source source, string id, List<ExportItem> items)
    {
        foreach (var item in items)
        {
            // Una categoría que no está en la lista haría que otro grupo rechace el archivo completo
            item.Category = NormalizeCategory(item.Category);
            if (item.Url is not null)
                item.Url = CleanUrl(item.Url);
        }

        return new ExportSource
        {
            Id = id,
            Name = source.Name,
            Url = CleanUrl(source.Url),
            Category = items.Select(i => i.Category).FirstOrDefault(c => c is not null),
            Description = source.Description,
            ComponentType = source.ComponentType,
            RequiresSecret = source.RequiresSecret,
            Items = items
        };
    }

    private static string BuildFile(List<ExportSource> sources)
    {
        var export = new DashboardExport { ExportedAt = DateTime.UtcNow, ExportedBy = GroupName, Sources = sources };

        // Saltos de línea siempre iguales (solo LF), sin importar el sistema donde se genere el archivo
        var json = export.ToJson().Replace(Environment.NewLine, "\n");

        // Antes de compartir un archivo se valida contra el schema (regla del formato común)
        var errors = Validate(JsonNode.Parse(json));
        if (errors.Count > 0)
            throw ApiException.Conflict("El archivo no cumple el formato común: " + string.Join(" ", errors));

        return json;
    }

    public async Task<ImportResultDto> ImportAsync(IFormFile? file, bool preview)
    {
        if (file is null || file.Length == 0)
            throw ApiException.BadRequest("Elige un archivo .json para importar.");
        if (file.Length > MaxFileBytes)
            throw ApiException.BadRequest("El archivo es muy grande (máximo 5 MB).");

        var root = await ReadJsonAsync(file);
        var result = new ImportResultDto { IsPreview = preview };

        // Una categoría desconocida no rechaza el archivo: se guarda como "otros" y se avisa
        result.UnknownCategories = NormalizeCategories(root);

        var errors = Validate(root);
        if (errors.Count > 0)
            throw ApiException.BadRequest("El archivo no cumple el formato común 1.0. " + string.Join(" ", errors));

        var export = DashboardExport.FromJson(root.ToJsonString())
            ?? throw ApiException.BadRequest("El archivo está vacío.");

        result.Version = export.Version;
        result.SourcesInFile = export.Sources.Count;
        result.ItemsInFile = export.Sources.Sum(s => s.Items.Count);

        foreach (var exportSource in export.Sources)
        {
            var sourceId = await FindOrCreateSourceAsync(exportSource, result, preview);

            foreach (var item in exportSource.Items)
            {
                // En la vista previa, una fuente nueva todavía no tiene id: todas sus fichas serían nuevas
                var existing = sourceId is int id ? await _items.FindByExternalIdAsync(id, item.Id) : null;

                if (existing is null)
                {
                    if (!preview)
                    {
                        var json = JsonSerializer.Serialize(item, CompactOptions);
                        await _items.CreateAsync(new SourceItem { SourceId = sourceId!.Value, Json = json, CreatedAt = DateTime.UtcNow });
                    }
                    result.ItemsCreated++;
                }
                else
                {
                    if (!preview)
                    {
                        existing.Json = JsonSerializer.Serialize(item, CompactOptions);
                        existing.CreatedAt = DateTime.UtcNow;
                        await _items.UpdateAsync(existing);
                    }
                    result.ItemsReplaced++;
                    result.ReplacedItems.Add($"{exportSource.Id}, {item.Id}");
                }
            }
        }

        return result;
    }

    // La fuente se busca por su URL (o por el nombre si el archivo no trae URL). Si no existe, se crea.
    // En la vista previa no se crea nada: se devuelve null y solo se anota en el resultado.
    private async Task<int?> FindOrCreateSourceAsync(ExportSource exportSource, ImportResultDto result, bool preview)
    {
        var url = exportSource.Url?.Trim() ?? string.Empty;

        Source? source;
        if (url.Length > 0)
            source = await _sources.FindByUrlAsync(url);
        else
            source = (await _sources.ReadAsync()).FirstOrDefault(s =>
                s.Name.Equals(exportSource.Name, StringComparison.OrdinalIgnoreCase));

        if (source is not null)
            return source.Id;

        source = new Source
        {
            Name = Truncate(exportSource.Name.Trim(), 200),
            Url = Truncate(url, 500),
            Description = exportSource.Description is null ? null : Truncate(exportSource.Description.Trim(), 500),
            ComponentType = exportSource.ComponentType is "widget" or "api" or "feed" ? exportSource.ComponentType : "api",
            RequiresSecret = exportSource.RequiresSecret ?? false
        };
        result.CreatedSources.Add(source.Name);

        if (preview)
            return null;

        await _sources.CreateAsync(source);
        return source.Id;
    }

    // Lee el archivo como UTF-8 estricto (el formato lo exige) y lo convierte en JSON
    private static async Task<JsonNode> ReadJsonAsync(IFormFile file)
    {
        byte[] bytes;
        using (var stream = file.OpenReadStream())
        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory);
            bytes = memory.ToArray();
        }

        // Un archivo UTF-8 puede empezar con la marca BOM (3 bytes), que no es parte del JSON
        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;

        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            throw ApiException.BadRequest("El archivo no está codificado en UTF-8.");
        }

        try
        {
            return JsonNode.Parse(text) ?? throw ApiException.BadRequest("El archivo está vacío.");
        }
        catch (JsonException ex)
        {
            throw ApiException.BadRequest($"El archivo no es un JSON válido: {ex.Message}");
        }
    }

    // Devuelve los primeros errores del schema, con la ruta del campo, para que el usuario sepa qué corregir
    private static List<string> Validate(JsonNode? json)
    {
        var results = Schema.Value.Evaluate(json, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (results.IsValid)
            return new List<string>();

        return results.Details
            .Where(d => !d.IsValid && d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!.Values.Select(message =>
            {
                var path = d.InstanceLocation.ToString() is { Length: > 0 } location ? location : "/";
                return $"{path}: {Translate(message, path)}.";
            }))
            .Distinct()
            .Take(5)
            .ToList();
    }

    // Los mensajes de la librería vienen en inglés: se resumen en español para quien sube el archivo
    private static string Translate(string message, string path)
    {
        if (message.Contains("regular expression", StringComparison.OrdinalIgnoreCase))
            return path.EndsWith("At", StringComparison.Ordinal)
                ? "la fecha debe estar en UTC y terminar en Z (por ejemplo 2026-10-14T06:00:00Z)"
                : "no tiene el formato esperado (en el id de la fuente solo van minúsculas, números y guiones)";
        if (message.Contains("values specified by the enum", StringComparison.OrdinalIgnoreCase))
            return "no es uno de los valores permitidos";
        if (message.StartsWith("Expected", StringComparison.OrdinalIgnoreCase))
            return "el valor no es el esperado (la versión debe ser \"1.0\")";
        if (message.Contains("required", StringComparison.OrdinalIgnoreCase))
            return "falta un campo obligatorio";
        if (message.Contains("Value is", StringComparison.OrdinalIgnoreCase) || message.Contains("type", StringComparison.OrdinalIgnoreCase))
            return "el tipo de dato no es el correcto";
        return message.TrimEnd('.');
    }

    // Cambia las categorías que no están en la lista por "otros" y devuelve cuántas cambió
    private static int NormalizeCategories(JsonNode root)
    {
        var changed = 0;

        void Fix(JsonNode? node)
        {
            if (node is not JsonObject obj || obj["category"] is not JsonValue value || !value.TryGetValue<string>(out var category))
                return;

            var normalized = NormalizeCategory(category);
            if (!string.Equals(normalized, category, StringComparison.Ordinal))
            {
                obj["category"] = normalized;
                if (!ExportCategories.All.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
                    changed++;
            }
        }

        if (root is JsonObject { } top && top["sources"] is JsonArray sources)
        {
            foreach (var source in sources)
            {
                Fix(source);
                if (source is JsonObject sourceObj && sourceObj["items"] is JsonArray items)
                    foreach (var item in items)
                        Fix(item);
            }
        }

        return changed;
    }

    private static string? NormalizeCategory(string? category)
    {
        if (category is null)
            return null;
        var match = ExportCategories.All.FirstOrDefault(c => c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? "otros";
    }

    private static string CleanUrl(string url) => SecretParams.Replace(url, string.Empty).TrimEnd('?', '&');

    // "Spaceflight News" -> "spaceflight-news" (el formato pide minúsculas, números y guiones)
    private static string Slugify(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray();
        var slug = Regex.Replace(new string(chars), "-+", "-").Trim('-');
        return slug.Length == 0 ? "fuente" : slug;
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];
}
