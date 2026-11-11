using Jason.Models.Dtos;

namespace Jason.Web.Models;

// Datos del panel "archivo para compartir" (partial _SharePanel)
public class ShareViewModel
{
    public int ItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}

// Pantalla de importar: o trae el resultado, o trae el error que explica qué falló
public class ImportViewModel
{
    public string? FileName { get; set; }
    public ImportResultDto? Result { get; set; }

    // Identifica el archivo que quedó en espera de confirmación (solo en la vista previa)
    public string? Token { get; set; }
    public string? Error { get; set; }
}
