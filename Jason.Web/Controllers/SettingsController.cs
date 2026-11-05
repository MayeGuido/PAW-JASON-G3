using System.Security.Claims;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Jason.Web.Models;
using Jason.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.Web.Controllers;

// Página de configuración: asignar roles y guardar secrets. Solo para el Admin.
[Authorize(Roles = Roles.Admin)]
public class SettingsController : Controller
{
    private const string ConnectionError =
        "No se pudo conectar con la API. Verifica que Jason.API esté corriendo y que la base de datos exista.";

    private readonly IJasonApiClient _api;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IJasonApiClient api, ILogger<SettingsController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new SettingsViewModel
        {
            CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty
        };

        try
        {
            model.Users = await _api.GetUsersAsync();
            model.Secrets = await _api.GetSecretsAsync();
            model.Sources = await _api.GetSourcesAsync();
        }
        catch (ApiClientException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar la configuración");
            ViewBag.Error = ConnectionError;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        await RunAsync(async () =>
        {
            await _api.ChangeRoleAsync(id, role);
            TempData["Success"] = "El rol se actualizó.";
        });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SaveSecret(SaveSecretViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        await RunAsync(async () =>
        {
            await _api.SaveSecretAsync(new SaveSecretDto { Key = model.Key, Value = model.Value, SourceId = model.SourceId });
            TempData["Success"] = $"El secret {model.Key} se guardó cifrado.";
        });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSecret(string key)
    {
        await RunAsync(async () =>
        {
            await _api.DeleteSecretAsync(key);
            TempData["Success"] = $"El secret {key} se eliminó.";
        });
        return RedirectToAction(nameof(Index));
    }

    // Las acciones de esta página comparten el mismo manejo de errores: se avisa con un mensaje y se vuelve a la lista
    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ApiClientException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Falló una acción de configuración");
            TempData["Error"] = ConnectionError;
        }
    }
}
