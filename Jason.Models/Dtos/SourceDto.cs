namespace Jason.Models.Dtos;

public class SourceDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public bool RequiresSecret { get; set; }
}
