using System.Text;
using Jason.Web.Models;
using Jason.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.Web.Controllers;

// Descargar una ficha como archivo JSON e importar archivos de otros grupos (formato común).
// Hace falta sesión: la API pide el token para exportar e importar.
[Authorize]
public class ExchangeController : Controller
{
    private const string ConnectionError =
        "No se pudo conectar con la API. Verifica que Jason.API esté corriendo y que la base de datos exista.";

    private readonly IJasonApiClient _api;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(IJasonApiClient api, ILogger<ExchangeController> logger)
    {
        _api = api;
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

    // Descarga el .json de la ficha (UTF-8 sin BOM)
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var file = await _api.ExportItemAsync(id);
            return File(new UTF8Encoding(false).GetBytes(file.Json), "application/json", file.FileName);
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
            _logger.LogError(ex, "No se pudo descargar la ficha {ItemId}", id);
            TempData["Error"] = ConnectionError;
            return RedirectToAction(nameof(Import));
        }
    }

    [HttpGet]
    public IActionResult Import() => View(new ImportViewModel());

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        var model = new ImportViewModel { FileName = file?.FileName };

        if (file is null || file.Length == 0)
        {
            model.Error = "Elige un archivo .json para importar.";
            return View(model);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            model.Result = await _api.ImportFileAsync(stream, file.FileName);
        }
        catch (ApiClientException ex)
        {
            // La API explica qué no cuadra en el archivo: se le muestra tal cual a quien lo subió
            model.Error = ex.Message;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo importar el archivo {FileName}", file.FileName);
            model.Error = ConnectionError;
        }

        return View(model);
    }
}
