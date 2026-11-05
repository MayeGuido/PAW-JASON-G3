using System.ComponentModel.DataAnnotations;

namespace Jason.Models.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El nombre puede tener hasta 100 caracteres.")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = string.Empty;
}

// Respuesta de login y registro: el token JWT, cuándo vence (UTC) y los datos del usuario
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class ChangeRoleDto
{
    [Required(ErrorMessage = "El rol es obligatorio.")]
    [RegularExpression("^(Admin|User)$", ErrorMessage = "El rol debe ser Admin o User.")]
    public string Role { get; set; } = string.Empty;
}
