using Jason.API.Exceptions;
using Jason.DataAccess.Identity;
using Jason.Models.Constants;
using Jason.Models.Dtos;
using Microsoft.AspNetCore.Identity;

namespace Jason.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetCurrentAsync(string userId);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            throw ApiException.Conflict("Ya existe una cuenta con ese correo.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim()
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            throw ApiException.BadRequest(string.Join(" ", created.Errors.Select(e => e.Description)));

        // Las cuentas nuevas siempre empiezan como User. Solo un Admin puede subir de rol.
        await _userManager.AddToRoleAsync(user, Roles.User);

        return BuildResponse(user, Roles.User);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // El mismo mensaje para correo inexistente y contraseña incorrecta, para no revelar qué cuentas existen
        var invalid = ApiException.Unauthorized("Correo o contraseña incorrectos.");

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null)
            throw invalid;

        if (await _userManager.IsLockedOutAsync(user))
            throw ApiException.Locked("La cuenta está bloqueada temporalmente por intentos fallidos. Intenta de nuevo en unos minutos.");

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            await _userManager.AccessFailedAsync(user);
            throw invalid;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return BuildResponse(user, PickRole(roles));
    }

    public async Task<UserDto> GetCurrentAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw ApiException.NotFound("El usuario ya no existe.");

        return ToDto(user, PickRole(await _userManager.GetRolesAsync(user)));
    }

    // Si alguien tuviera los dos roles, gana Admin
    private static string PickRole(IList<string> roles) =>
        roles.Contains(Roles.Admin) ? Roles.Admin : Roles.User;

    private AuthResponseDto BuildResponse(ApplicationUser user, string role)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user, role);
        return new AuthResponseDto { Token = token, ExpiresAt = expiresAt, User = ToDto(user, role) };
    }

    internal static UserDto ToDto(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        Role = role
    };
}
