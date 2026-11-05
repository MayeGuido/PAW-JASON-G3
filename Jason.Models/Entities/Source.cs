namespace Jason.Models.Entities;

// Tabla Sources definida por el profesor. No cambiar columnas existentes: el JSON se comparte entre grupos.
public class Source
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // widget, api o feed
    public string ComponentType { get; set; } = "api";
    public bool RequiresSecret { get; set; }

    public ICollection<SourceItem> Items { get; set; } = new List<SourceItem>();
}
