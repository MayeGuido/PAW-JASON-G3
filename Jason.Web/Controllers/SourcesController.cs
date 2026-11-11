using Jason.Web.Services;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.Web.Controllers;

public class SourcesController : Controller
{
    private readonly IJasonApiClient _api;
    private readonly ILogger<SourcesController> _logger;

    public SourcesController(IJasonApiClient api, ILogger<SourcesController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _api.GetSourcesAsync());
        }
        catch (HttpRequestException ex)
        {
            // Error amigable en vez de una página 500
            _logger.LogError(ex, "No se pudo consultar la API de fuentes");
            ViewBag.Error = "No se pudo conectar con la API. Verifica que Jason.API esté corriendo y que la base de datos exista.";
            return View(Array.Empty<Jason.Models.Dtos.SourceDto>());
        }
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CreateSourceDto source)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _api.CreateSourceAsync(source);
            TempData["Success"] = "La fuente se agregó correctamente.";
        }
        catch (ApiClientException exception)
        {
            TempData["Error"] = exception.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "No se pudo agregar la fuente");
            TempData["Error"] = "No se pudo conectar con la API.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _api.DeleteSourceAsync(id);
            TempData["Success"] = "La fuente se eliminó correctamente.";
        }
        catch (ApiClientException exception)
        {
            TempData["Error"] = exception.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "No se pudo eliminar la fuente {SourceId}", id);
            TempData["Error"] = "No se pudo conectar con la API.";
        }

        return RedirectToAction(nameof(Index));
    }
}
