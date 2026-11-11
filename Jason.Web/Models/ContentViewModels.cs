using System.Net;
using System.Text.RegularExpressions;
using Jason.Models.Exchange;

namespace Jason.Web.Models;

// Pantalla "Contenido": las fichas del feed o las ya guardadas.
public class ContentViewModel
{
    public const string TabFeed = "feed";
    public const string TabSaved = "saved";

    // "feed" (Desde las fuentes) o "saved" (Guardados)
    public string Tab { get; set; } = TabFeed;

    // Filtro por tipo (text, image, gif, video, audio). Null = todos.
    public string? Type { get; set; }

    public int SavedCount { get; set; }

    // Se pidieron los guardados pero no hay ninguno: se muestran las fichas de las fuentes
    public bool FellBackToFeed { get; set; }

    // Cuántas fichas había antes de recortar la lista
    public int TotalCards { get; set; }

    public string? Error { get; set; }

    public List<ContentCardViewModel> Cards { get; set; } = new();

    public static readonly (string? Value, string Label)[] Types =
    {
        (null, "Todos"),
        (MediaTypes.Text, "Texto"),
        (MediaTypes.Image, "Imagen"),
        (MediaTypes.Gif, "GIF"),
        (MediaTypes.Video, "Video"),
        (MediaTypes.Audio, "Audio")
    };
}

public class ContentCardViewModel
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public ExportItem Item { get; set; } = new();
    public bool IsSaved { get; set; }

    // Id de la fila en SourceItems. Solo lo tienen las fichas que vienen de la pestaña Guardados.
    public int? SavedItemId { get; set; }

    // El contenido viene de APIs externas: solo se enlazan URLs http o https (nunca javascript: u otras)
    public string? SafeUrl => ToSafeUrl(Item.Url);
    public string? SafeThumbnail => ToSafeUrl(Item.Thumbnail);

    // Descripción sin etiquetas HTML y recortada para que las tarjetas queden parejas
    public string? ShortDescription
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Item.Description))
                return null;

            string text = WebUtility.HtmlDecode(Regex.Replace(Item.Description, "<[^>]*>", " "));
            text = Regex.Replace(text, @"\s+", " ").Trim();
            if (text.Length == 0)
                return null;

            return text.Length <= 200 ? text : text.Substring(0, 200).TrimEnd() + "…";
        }
    }

    private static string? ToSafeUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return null;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;
    }
}
