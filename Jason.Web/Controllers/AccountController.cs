using System.Security.Claims;
using Jason.Models.Dtos;
using Jason.Web.Models;
using Jason.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.Web.Controllers;

// Login y registro: la Web le pregunta a la API y guarda el token JWT dentro de la cookie de sesión
public class AccountController : Controller
{
    private const string ConnectionError =
        "No se pudo conectar con la API. Verifica que Jason.API esté corriendo y que la base de datos exista.";

    private readonly IJasonApiClient _api;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IJasonApiClient api, ILogger<AccountController> logger)
    {
        _api = api;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var auth = await _api.LoginAsync(new LoginDto { Email = model.Email, Password = model.Password });
            await SignInAsync(auth);
            TempData["Success"] = $"Hola, {auth.User.DisplayName ?? auth.User.Email}.";
            return RedirectAfterLogin(model.ReturnUrl);
        }
        catch (ApiClientException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo llamar a la API en el login");
            ModelState.AddModelError(string.Empty, ConnectionError);
        }

        model.Password = string.Empty;
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var auth = await _api.RegisterAsync(new RegisterDto
            {
                Email = model.Email,
                DisplayName = model.DisplayName,
                Password = model.Password
            });
            await SignInAsync(auth);
            TempData["Success"] = "Tu cuenta se creó. Ya puedes guardar fichas.";
            return RedirectToAction("Index", "Home");
        }
        catch (ApiClientException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo llamar a la API en el registro");
            ModelState.AddModelError(string.Empty, ConnectionError);
        }

        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Cerraste sesión.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id),
            new(ClaimTypes.Email, auth.User.Email),
            new(ClaimTypes.Name, auth.User.DisplayName ?? auth.User.Email),
            new(ClaimTypes.Role, auth.User.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // La sesión de la Web vence cuando vence el token de la API
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(auth.ExpiresAt, DateTimeKind.Utc))
        };
        properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = auth.Token } });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
