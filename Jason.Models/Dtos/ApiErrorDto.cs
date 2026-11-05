namespace Jason.Models.Dtos;

// Formato único de error de la API
public class ApiErrorDto
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
