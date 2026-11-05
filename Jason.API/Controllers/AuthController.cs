using System.Security.Claims;
using Jason.API.Services;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // Crea una cuenta con rol User y devuelve el token, como si ya hubiera iniciado sesión
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        => Ok(await _authService.RegisterAsync(dto));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        => Ok(await _authService.LoginAsync(dto));

    // Datos de quien tiene la sesión (sirve para comprobar que el token sigue siendo válido)
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
        => Ok(await _authService.GetCurrentAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
}
