using System.ComponentModel.DataAnnotations;

namespace Jason.Models.Dtos;

// Lo que la API devuelve de un secret: nunca el valor completo, solo una versión enmascarada
public class SecretDto
{
    public string Key { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public string MaskedValue { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class SaveSecretDto
{
    [Required(ErrorMessage = "El nombre del secret es obligatorio.")]
    [RegularExpression(@"^[A-Za-z0-9_.:\-]{1,200}$",
        ErrorMessage = "El nombre solo puede tener letras, números y los símbolos _ . : - (máximo 200).")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "El valor es obligatorio.")]
    [StringLength(2000, ErrorMessage = "El valor puede tener hasta 2000 caracteres.")]
    public string Value { get; set; } = string.Empty;

    // Si el secret pertenece a una fuente en particular
    public int? SourceId { get; set; }
}
