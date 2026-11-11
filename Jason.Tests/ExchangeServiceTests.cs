using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jason.API.Exceptions;
using Jason.API.Services;
using Jason.Models.Entities;
using Jason.Models.Exchange;
using Jason.Tests.Fakes;
using Microsoft.AspNetCore.Http;

namespace Jason.Tests;

// Pruebas de descargar e importar archivos del formato común (ExchangeService), sin base de datos.
public class ExchangeServiceTests
{
    private const string SpaceUrl = "https://api.spaceflightnewsapi.net/v4/articles";

    private readonly FakeSourceRepository _sources = new();
    private readonly FakeSourceItemRepository _items = new();
    private readonly ExchangeService _service;

    public ExchangeServiceTests()
    {
        _service = new ExchangeService(_sources, _items);

        // Una fuente con una ficha guardada, como la que queda al guardar una ficha desde el feed
        _sources.Data.Add(new Source { Id = 1, Name = "Spaceflight News", Url = SpaceUrl, ComponentType = "api" });
        _items.Data.Add(new SourceItem
        {
            Id = 1,
            SourceId = 1,
            CreatedAt = DateTime.UtcNow,
            Json = """
                {"id":"12345","title":"Lanzamiento exitoso","mediaType":"text","createdAt":"2026-10-14T06:00:00Z",
                 "category":"ciencia","url":"https://api.spaceflightnewsapi.net/v4/articles/12345?api_key=SECRETO123&lang=es",
                 "data":{"news_site":"SpaceNews","summary":"Ñandú y órbita"}}
                """
        });
    }

    // ---------- Descargar ----------

    [Fact]
    public async Task ExportItem_QuitaLaApiKeyDeLaUrl_YElArchivoCumpleElFormato()
    {
        var file = await _service.ExportItemAsync(1);

        Assert.Equal("spaceflight-news-12345.json", file.FileName);
        Assert.DoesNotContain("SECRETO123", file.Json);
        Assert.Contains("?lang=es", file.Json);
        Assert.Contains("Ñandú", file.Json);
        Assert.DoesNotContain('\r', file.Json);

        var export = DashboardExport.FromJson(file.Json)!;
        Assert.Equal("1.0", export.Version);
        Assert.Equal("spaceflight-news", export.Sources.Single().Id);
        Assert.Equal(new DateTime(2026, 10, 14, 6, 0, 0, DateTimeKind.Utc), export.Sources[0].Items[0].CreatedAt);
    }

    [Fact]
    public async Task ExportItem_ConFichaInexistente_DaNotFound()
    {
        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ExportItemAsync(999));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ExportItem_CategoriaFueraDeLaLista_SaleComoOtros()
    {
        _items.Data[0].Json = _items.Data[0].Json.Replace("\"ciencia\"", "\"cosas-raras\"");

        var file = await _service.ExportItemAsync(1);

        Assert.Equal("otros", DashboardExport.FromJson(file.Json)!.Sources[0].Items[0].Category);
    }

    [Fact]
    public async Task ExportAll_AgrupaLasFichasPorFuente()
    {
        _sources.Data.Add(new Source { Id = 2, Name = "GIPHY", Url = "https://api.giphy.com", ComponentType = "api" });
        _items.Data.Add(new SourceItem { Id = 2, SourceId = 2, CreatedAt = DateTime.UtcNow, Json = ItemJson("g1", "Gato", "gifs", "gif") });
        _items.Data.Add(new SourceItem { Id = 3, SourceId = 2, CreatedAt = DateTime.UtcNow, Json = ItemJson("g2", "Perro", "gifs", "gif") });

        var export = DashboardExport.FromJson((await _service.ExportAllAsync()).Json)!;

        Assert.Equal(2, export.Sources.Count);
        Assert.Single(export.Sources.Single(s => s.Id == "spaceflight-news").Items);
        Assert.Equal(2, export.Sources.Single(s => s.Id == "giphy").Items.Count);
    }

    [Fact]
    public async Task ExportAll_SinFichasGuardadas_DaNotFound()
    {
        _items.Data.Clear();

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ExportAllAsync());
        Assert.Equal(404, ex.StatusCode);
    }

    // ---------- Importar ----------

    [Fact]
    public async Task Import_ArchivoDeOtroGrupo_CreaLaFuenteYLasFichas()
    {
        var result = await _service.ImportAsync(File(OtherGroupFile()), preview: false);

        Assert.False(result.IsPreview);
        Assert.Equal(2, result.ItemsCreated);
        Assert.Equal(0, result.ItemsReplaced);
        Assert.Equal(new[] { "GIPHY" }, result.CreatedSources);
        Assert.Contains(_sources.Data, s => s.Name == "GIPHY" && s.Url == "https://api.giphy.com/v1/gifs/trending");
        Assert.Equal(3, _items.Data.Count);
    }

    [Fact]
    public async Task Import_ElMismoArchivoDosVeces_ReemplazaEnVezDeDuplicar()
    {
        await _service.ImportAsync(File(OtherGroupFile()), preview: false);
        var second = await _service.ImportAsync(File(OtherGroupFile()), preview: false);

        Assert.Equal(0, second.ItemsCreated);
        Assert.Equal(2, second.ItemsReplaced);
        Assert.Equal(new[] { "giphy, g1", "giphy, g2" }, second.ReplacedItems);
        Assert.Equal(3, _items.Data.Count);
        Assert.Equal(2, _sources.Data.Count);
    }

    [Fact]
    public async Task Import_CategoriaDesconocida_SeGuardaComoOtrosYSeAvisa()
    {
        var file = FileWith(ItemJson("x1", "Algo", "cosas-raras", "text"));

        var result = await _service.ImportAsync(File(file), preview: false);

        Assert.Equal(1, result.UnknownCategories);
        Assert.Contains("\"category\":\"otros\"", _items.Data.Last().Json);
    }

    [Fact]
    public async Task Import_FechaSinZ_EsRechazadaYNoGuardaNada()
    {
        var file = FileWith(ItemJson("x1", "Algo", "ciencia", "text", createdAt: "2026-10-01T12:00:00"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ImportAsync(File(file), preview: false));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("UTC", ex.Message);
        Assert.Single(_items.Data);
    }

    [Fact]
    public async Task Import_VersionDistintaDeLa1_EsRechazada()
    {
        var file = OtherGroupFile().Replace("\"version\":\"1.0\"", "\"version\":\"2.0\"");

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ImportAsync(File(file), preview: false));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("/version", ex.Message);
    }

    [Fact]
    public async Task Import_ArchivoQueNoEsJson_EsRechazado()
    {
        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ImportAsync(File("esto no es json"), preview: false));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("JSON", ex.Message);
    }

    [Fact]
    public async Task Import_ArchivoQueNoEsUtf8_EsRechazado()
    {
        var bytes = new byte[] { 0x7B, 0xFF, 0xFE, 0x7D };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ImportAsync(File(bytes), preview: false));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("UTF-8", ex.Message);
    }

    [Fact]
    public async Task Import_ArchivoConBom_SeLeeBien()
    {
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(OtherGroupFile())).ToArray();

        var result = await _service.ImportAsync(File(bytes), preview: false);

        Assert.Equal(2, result.ItemsCreated);
    }

    [Fact]
    public async Task Import_SinArchivo_EsRechazado()
    {
        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ImportAsync(null, preview: false));

        Assert.Equal(400, ex.StatusCode);
    }

    // ---------- Vista previa ----------

    [Fact]
    public async Task Import_ConVistaPrevia_DiceQuePasariaPeroNoGuardaNada()
    {
        var result = await _service.ImportAsync(File(OtherGroupFile()), preview: true);

        Assert.True(result.IsPreview);
        Assert.Equal(2, result.ItemsCreated);
        Assert.Equal(new[] { "GIPHY" }, result.CreatedSources);
        Assert.Single(_sources.Data);
        Assert.Single(_items.Data);
    }

    [Fact]
    public async Task Import_VistaPreviaDeUnaFichaQueYaExiste_LaMarcaComoReemplazo()
    {
        var export = (await _service.ExportItemAsync(1)).Json;

        var result = await _service.ImportAsync(File(export), preview: true);

        Assert.Equal(0, result.ItemsCreated);
        Assert.Equal(1, result.ItemsReplaced);
        Assert.Equal(new[] { "spaceflight-news, 12345" }, result.ReplacedItems);
    }

    // ---------- Ida y vuelta ----------

    [Fact]
    public async Task IdaYVuelta_UnaFichaDescargadaSePuedeVolverAImportarIgual()
    {
        var before = DashboardExport.FromJson((await _service.ExportItemAsync(1)).Json)!.Sources[0].Items[0];
        var exported = (await _service.ExportItemAsync(1)).Json;

        // Se borra la ficha y se vuelve a cargar desde el archivo
        _items.Data.Clear();
        var result = await _service.ImportAsync(File(exported), preview: false);

        Assert.Equal(1, result.ItemsCreated);
        Assert.Empty(result.CreatedSources);

        var after = JsonSerializer.Deserialize<ExportItem>(_items.Data.Single().Json, DashboardExport.JsonOptions)!;
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Title, after.Title);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        // El contenido original de la API (data) tiene que quedar igual, aunque cambie el espaciado
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(before.Data!.Value.GetRawText()), JsonNode.Parse(after.Data!.Value.GetRawText())));
    }

    // ---------- Ayudas ----------

    private static string ItemJson(string id, string title, string category, string mediaType, string createdAt = "2026-10-19T08:00:00Z") =>
        $$"""{"id":"{{id}}","title":"{{title}}","mediaType":"{{mediaType}}","createdAt":"{{createdAt}}","category":"{{category}}","data":{"x":1} """.TrimEnd() + "}";

    // Archivo con una sola fuente nueva y los items indicados
    private static string FileWith(params string[] items) =>
        """{"version":"1.0","sources":[{"id":"otra-fuente","name":"Otra fuente","url":"https://otra.example/api","items":[""" +
        string.Join(",", items) + "]}]}";

    // Archivo parecido al que mandaría otro grupo: una fuente que no existe aquí y 2 items
    private static string OtherGroupFile() =>
        """{"version":"1.0","exportedAt":"2026-10-20T10:00:00Z","exportedBy":"Grupo 9","sources":[{"id":"giphy","name":"GIPHY","url":"https://api.giphy.com/v1/gifs/trending","category":"gifs","campoExtra":"se ignora","items":[""" +
        ItemJson("g1", "Gato bailando", "gifs", "gif") + "," + ItemJson("g2", "Perro", "gifs", "gif") + "]}]}";

    private static IFormFile File(string content) => File(Encoding.UTF8.GetBytes(content));

    private static IFormFile File(byte[] bytes) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "archivo.json");
}
