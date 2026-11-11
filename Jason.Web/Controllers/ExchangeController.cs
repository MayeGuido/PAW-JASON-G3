using System.Security.Claims;
using System.Text;
using Jason.Web.Models;
using Jason.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Jason.Web.Controllers;

// Descargar fichas como archivo JSON e importar archivos de otros grupos (formato común).
// Hace falta sesión: la API pide el token para exportar e importar.
[Authorize]
public class ExchangeController : Controller
{
    private const string ConnectionError =
        "No se pudo conectar con la API. Verifica que Jason.API esté corriendo y que la base de datos exista.";

    private const long MaxFileBytes = 5 * 1024 * 1024;
    private static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(10);

    private readonly IJasonApiClient _api;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(IJasonApiClient api, IMemoryCache cache, ILogger<ExchangeController> logger)
    {
        _api = api;
        _cache = cache;
        _logger = logger;
    }

    // Muestra el panel "archivo para compartir" de una ficha.
    // TODO: cuando exista Items/Details (tramo C), ese detalle incluye este mismo partial (_SharePanel).
    public async Task<IActionResult> Share(int id)
    {
        try
        {
            var file = await _api.ExportItemAsync(id);
            return View(new ShareViewModel { ItemId = id, FileName = file.FileName, Json = file.Json });
        }
        catch (ApiClientException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Import));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo armar el archivo para compartir la ficha {ItemId}", id);
            TempData["Error"] = ConnectionError;
            return RedirectToAction(nameof(Import));
        }
    }

    // Descarga el .json de una ficha (UTF-8 sin BOM)
    public Task<IActionResult> Download(int id) =>
        DownloadAsync(() => _api.ExportItemAsync(id), "No se pudo descargar la ficha {ItemId}", id);

    // Descarga un solo .json con todas las fichas guardadas
    public Task<IActionResult> DownloadAll() =>
        DownloadAsync(() => _api.ExportAllAsync(), "No se pudieron descargar todas las fichas", 0);

    [HttpGet]
    public IActionResult Import() => View(new ImportViewModel());

    // Paso 1: la API revisa el archivo y dice qué pasaría, pero todavía no guarda nada.
    // El archivo se queda unos minutos en memoria para confirmarlo sin volver a subirlo.
    [HttpPost]
    public async Task<IActionResult> Preview(IFormFile? file)
    {
        var model = new ImportViewModel { FileName = file?.FileName };

        if (file is null || file.Length == 0)
        {
            model.Error = "Elige un archivo .json para importar.";
            return View(nameof(Import), model);
        }
        if (file.Length > MaxFileBytes)
        {
            model.Error = "El archivo es muy grande (máximo 5 MB).";
            return View(nameof(Import), model);
        }

        using var memory = new MemoryStream();
        await using (var stream = file.OpenReadStream())
            await stream.CopyToAsync(memory);
        var content = memory.ToArray();

        await RunImportAsync(model, content, file.FileName, preview: true);

        if (model.Result is not null)
        {
            model.Token = Guid.NewGuid().ToString("N");
            _cache.Set(PendingKey(model.Token), new PendingImport(file.FileName, content), PendingLifetime);
        }

        return View(nameof(Import), model);
    }

    // Paso 2: el usuario confirmó, ahora sí se guarda
    [HttpPost]
    public async Task<IActionResult> Confirm(string token)
    {
        var model = new ImportViewModel();

        if (string.IsNullOrEmpty(token) || !_cache.TryGetValue(PendingKey(token), out PendingImport? pending) || pending is null)
        {
            model.Error = "La revisión del archivo venció. Vuelve a subirlo para importarlo.";
            return View(nameof(Import), model);
        }

        model.FileName = pending.FileName;
        await RunImportAsync(model, pending.Content, pending.FileName, preview: false);

        // Solo se olvida el archivo si se guardó; si falló, se puede volver a intentar
        if (model.Result is not null)
            _cache.Remove(PendingKey(token));

        return View(nameof(Import), model);
    }

    private async Task RunImportAsync(ImportViewModel model, byte[] content, string fileName, bool preview)
    {
        try
        {
            using var stream = new MemoryStream(content);
            model.Result = await _api.ImportFileAsync(stream, fileName, preview);
        }
        catch (ApiClientException ex)
        {
            // La API explica qué no cuadra en el archivo: se le muestra tal cual a quien lo subió
            model.Error = ex.Message;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo importar el archivo {FileName}", fileName);
            model.Error = ConnectionError;
        }
    }

    private async Task<IActionResult> DownloadAsync(Func<Task<Jason.Models.Dtos.ExportFileDto>> export, string logMessage, int itemId)
    {
        try
        {
            var file = await export();
            return File(new UTF8Encoding(false).GetBytes(file.Json), "application/json", file.FileName);
        }
        catch (ApiClientException ex) when (ex.StatusCode == StatusCodes.Status404NotFound && itemId > 0)
        {
            return NotFound();
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Import));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, logMessage, itemId);
            TempData["Error"] = ConnectionError;
            return RedirectToAction(nameof(Import));
        }
    }

    // El archivo pendiente es de quien lo subió: la llave incluye su usuario
    private string PendingKey(string token) => $"import:{User.FindFirstValue(ClaimTypes.NameIdentifier)}:{token}";

    private sealed record PendingImport(string FileName, byte[] Content);
}
