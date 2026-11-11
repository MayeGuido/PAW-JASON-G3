namespace Jason.Models.Dtos;

// Archivo JSON listo para descargar (formato común). El contenido va como texto UTF-8 sin BOM.
public class ExportFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}

// Resumen de lo que pasó al importar un archivo
public class ImportResultDto
{
    public string Version { get; set; } = string.Empty;
    public int SourcesInFile { get; set; }
    public int ItemsInFile { get; set; }

    public int ItemsCreated { get; set; }
    public int ItemsReplaced { get; set; }

    // Nombres de las fuentes que no existían y se crearon
    public List<string> CreatedSources { get; set; } = new();

    // "id-de-la-fuente, id-del-item" de cada ficha que ya existía y se reemplazó
    public List<string> ReplacedItems { get; set; } = new();

    // Fichas o fuentes con categoría desconocida (se guardaron como "otros")
    public int UnknownCategories { get; set; }
}
