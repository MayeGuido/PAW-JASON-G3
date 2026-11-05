namespace Jason.Models.Entities;

// Secrets y ajustes. El valor se guarda cifrado (Data Protection de ASP.NET).
public class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ValueEncrypted { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
