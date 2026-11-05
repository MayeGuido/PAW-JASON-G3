using System.ComponentModel.DataAnnotations;
using Jason.Models.Dtos;

namespace Jason.Web.Models;

public class SettingsViewModel
{
    public IReadOnlyList<UserDto> Users { get; set; } = Array.Empty<UserDto>();
    public IReadOnlyList<SecretDto> Secrets { get; set; } = Array.Empty<SecretDto>();
    public IReadOnlyList<SourceDto> Sources { get; set; } = Array.Empty<SourceDto>();
    public string CurrentUserId { get; set; } = string.Empty;
}

public class SaveSecretViewModel
{
    [Required(ErrorMessage = "El nombre del secret es obligatorio.")]
    [RegularExpression(@"^[A-Za-z0-9_.:\-]{1,200}$",
        ErrorMessage = "El nombre solo puede tener letras, números y los símbolos _ . : - (máximo 200).")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "El valor es obligatorio.")]
    [StringLength(2000, ErrorMessage = "El valor puede tener hasta 2000 caracteres.")]
    public string Value { get; set; } = string.Empty;

    public int? SourceId { get; set; }
}
