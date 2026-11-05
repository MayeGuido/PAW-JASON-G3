using System.ComponentModel.DataAnnotations;

namespace Jason.Models.Dtos;

public class CreateSourceDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La URL es obligatoria.")]
    [Url(ErrorMessage = "La URL no es válida.")]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(widget|api|feed)$", ErrorMessage = "El tipo debe ser widget, api o feed.")]
    public string ComponentType { get; set; } = "api";

    public bool RequiresSecret { get; set; }
}
