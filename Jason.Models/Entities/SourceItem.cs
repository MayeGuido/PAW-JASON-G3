namespace Jason.Models.Entities;

// Tabla SourceItems definida por el profesor. Json guarda el item completo del formato común.
public class SourceItem
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string Json { get; set; } = string.Empty;

    // Fecha en que se guardó (no es la fecha del contenido, esa va dentro del Json)
    public DateTime CreatedAt { get; set; }

    public Source? Source { get; set; }
}
