using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jason.Models.Exchange;

// Formato común de intercambio entre grupos, versión 1.0 (propuesta del grupo coordinador).
// Referencia: docs/formato-json.md. Serializar siempre con DashboardExport.JsonOptions.

public class DashboardExport
{
    public const string CurrentVersion = "1.0";

    [JsonPropertyName("version")]
    public string Version { get; set; } = CurrentVersion;

    [JsonPropertyName("exportedAt")]
    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ExportedAt { get; set; }

    [JsonPropertyName("exportedBy")]
    public string? ExportedBy { get; set; }

    [JsonPropertyName("sources")]
    public List<ExportSource> Sources { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static DashboardExport? FromJson(string json) =>
        JsonSerializer.Deserialize<DashboardExport>(json, JsonOptions);
}

public class ExportSource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Valores permitidos: ver ExportCategories.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    // Campos opcionales que proponemos agregar al formato (pendiente de acuerdo con los demás grupos).
    // Los otros grupos los ignoran, así que no rompen la compatibilidad. Corresponden a la tabla Sources del profesor.
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>widget, api o feed</summary>
    [JsonPropertyName("componentType")]
    public string? ComponentType { get; set; }

    [JsonPropertyName("requiresSecret")]
    public bool? RequiresSecret { get; set; }

    /// <summary>json, xml o html</summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("items")]
    public List<ExportItem> Items { get; set; } = new();
}

public class ExportItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Valores permitidos: ver MediaTypes.</summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = MediaTypes.Text;

    /// <summary>Fecha del contenido, siempre UTC. Si la API no trae fecha, usar la de captura.</summary>
    [JsonPropertyName("createdAt")]
    [JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Respuesta original de la API, como objeto JSON (nunca como string).</summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

public static class MediaTypes
{
    public const string Text = "text";
    public const string Image = "image";
    public const string Gif = "gif";
    public const string Video = "video";
    public const string Audio = "audio";

    public static readonly string[] All = { Text, Image, Gif, Video, Audio };
}

public static class ExportCategories
{
    public static readonly string[] All =
    {
        "noticias", "memes", "gifs", "imagenes", "videos", "musica", "libros",
        "clima", "deportes", "tecnologia", "entretenimiento", "finanzas", "ciencia", "otros"
    };
}
