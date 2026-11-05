using Jason.Web.Services;
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
}
