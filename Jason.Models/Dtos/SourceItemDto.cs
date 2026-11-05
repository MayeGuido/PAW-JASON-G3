namespace Jason.Models.Dtos;

public class SourceItemDto
{
    public int Id { get; set; }
    public int SourceId { get; set; }

    // Item completo en el formato común (JSON como texto)
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
