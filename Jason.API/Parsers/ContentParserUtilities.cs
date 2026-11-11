using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jason.Models.Exchange;

namespace Jason.API.Parsers;

// Métodos pequeños que comparten los tres parsers
// La idea es evitar copiar la misma lógica en JSON, XML y HTML
internal static class ContentParserUtilities
{
    // Lee un texto del item probando varios nombres posibles. Si no lo encuentra, busca también dentro de "properties", "attributes" o "fields".
    public static string? ReadString(JsonElement item, params string[] possibleNames) 
    {
        JsonElement value; 

        // Primero busca el campo directamente en el objeto
        if (TryFindProperty(item, possibleNames, out value))
            return ConvertToText(value);

        // Algunas APIs, como USGS, guardan los datos dentro de "properties"
        string[] containers = { "properties", "attributes", "fields" };
        foreach (string containerName in containers)
        {
            JsonElement container;
            if (TryFindProperty(item, new[] { containerName }, out container)
                && TryFindProperty(container, possibleNames, out value))
            {
                return ConvertToText(value);
            }
        }

        return null;
    }


    //Bueno este metodo lee la fecha 
    public static DateTime ReadDate(JsonElement item, DateTime fallback, params string[] possibleNames)
    {
        JsonElement value;

        if (!TryFindProperty(item, possibleNames, out value))
        {
            JsonElement properties;
            if (!TryFindProperty(item, new[] { "properties" }, out properties)
                || !TryFindProperty(properties, possibleNames, out value))
            {
                return fallback;
            }
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
        {
            try
            {
                // Open Library a veces solo trae el año
                if (number >= 1000 && number <= 9999)
                    return new DateTime((int)number, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                // Si el número es muy grande, viene en milisegundos
                if (number > 10_000_000_000)
                    return DateTimeOffset.FromUnixTimeMilliseconds(number).UtcDateTime;

                return DateTimeOffset.FromUnixTimeSeconds(number).UtcDateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return fallback;
            }
        }

        return ParseDate(ConvertToText(value), fallback);
    }

    public static DateTime ParseDate(string? text, DateTime fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        DateTimeOffset date;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out date))
        {
            return date.UtcDateTime;
        }

        int year;
        if (int.TryParse(text, out year) && year >= 1000 && year <= 9999)
            return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return fallback;
    }

    // Este metodo afirma que el valor sea un obejto cierto, si no lo es lo envuelve en un objeto con la propiedad "value"
    public static JsonElement AsObject(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
            return value.Clone();

        // El formato común exige que Data sea un objeto, nunca un texto o número suelto.
        var wrapper = new Dictionary<string, object?>();
        wrapper["value"] = JsonSerializer.Deserialize<object?>(value.GetRawText());
        return JsonSerializer.SerializeToElement(wrapper);
    }

    public static string StableId(string text)
    {
        // Se usa un hash para obtener siempre el mismo id a partir del mismo contenido.
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 10).ToLowerInvariant();
    }

    // Devuelve un id que no se repita dentro de la fuente: si ya se usó, le agrega un número al final (-2, -3...).
    public static string UniqueId(string id, HashSet<string> usedIds)
    {
        string baseId = string.IsNullOrWhiteSpace(id) ? StableId(Guid.NewGuid().ToString()) : id.Trim();
        string result = baseId;
        int copyNumber = 2;

        while (usedIds.Contains(result))
        {
            result = baseId + "-" + copyNumber;
            copyNumber++;
        }
        // Se registra el id para que el siguiente item no lo repita
        usedIds.Add(result);
        return result;
    }


    //Bueno este metodo ayuda a normalizar la categoria de algun contenido
    public static string NormalizeCategory(string? category, ExportSource source)
    {
        string text = CleanText(category);

        if (text == "news") return "noticias";
        if (text == "technology" || text == "tech") return "tecnologia";
        if (text == "science") return "ciencia";
        if (text == "sports" || text == "sport") return "deportes";
        if (text == "weather") return "clima";
        if (text == "finance" || text == "business") return "finanzas";
        if (text == "books" || text == "book") return "libros";
        if (text == "music") return "musica";
        if (text == "entertainment") return "entretenimiento";
        if (text == "image" || text == "images") return "imagenes";
        if (text == "video") return "videos";
        if (text == "gif") return "gifs";
        if (text == "meme") return "memes";

        if (ExportCategories.All.Contains(text))
            return text;

        // Si el item no trae categoría, se intenta deducir usando el nombre de la fuente.
        string sourceText = CleanText(source.Name + " " + source.Description);
        if (sourceText.Contains("science") || sourceText.Contains("ciencia")
            || sourceText.Contains("space") || sourceText.Contains("arxiv")
            || sourceText.Contains("earthquake"))
            return "ciencia";

        if (sourceText.Contains("technology") || sourceText.Contains("tecnologia")
            || sourceText.Contains("hacker"))
            return "tecnologia";

        if (sourceText.Contains("book") || sourceText.Contains("libro")
            || sourceText.Contains("library"))
            return "libros";

        if (sourceText.Contains("news") || sourceText.Contains("noticia"))
            return "noticias";

        return "otros";
    }

    public static string NormalizeMediaType(string? mediaType, string? url)
    {
        string text = CleanText(mediaType);

        if (text == "image" || text == "photo" || text.StartsWith("image/"))
            return MediaTypes.Image;
        if (text == "gif") return MediaTypes.Gif;
        if (text == "video" || text == "movie") return MediaTypes.Video;
        if (text == "audio" || text == "podcast" || text.StartsWith("audio/"))
            return MediaTypes.Audio;

        if (!string.IsNullOrWhiteSpace(url))
        {
            string path = url.ToLowerInvariant();
            if (path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".png")
                || path.EndsWith(".webp")) return MediaTypes.Image;
            if (path.EndsWith(".gif")) return MediaTypes.Gif;
            if (path.EndsWith(".mp4") || path.EndsWith(".webm")) return MediaTypes.Video;
            if (path.EndsWith(".mp3") || path.EndsWith(".wav")) return MediaTypes.Audio;
        }

        return MediaTypes.Text;
    }

    public static string PlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string withoutTags = Regex.Replace(html, "<[^>]+>", " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static bool TryFindProperty(JsonElement item, string[] possibleNames, out JsonElement value)
    {
        if (item.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in item.EnumerateObject())
            {
                foreach (string name in possibleNames)
                {
                    if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }
        }

        value = default;
        return false;
    }

    // Convierte un valor JSON simple (texto, número o booleano) a texto. Cualquier otro tipo devuelve null.
    private static string? ConvertToText(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String) return value.GetString(); 
        if (value.ValueKind == JsonValueKind.Number) return value.GetRawText();
        if (value.ValueKind == JsonValueKind.True) return "true";
        if (value.ValueKind == JsonValueKind.False) return "false";
        return null;
    }

    // Normaliza un texto para compararlo: sin espacios en los extremos, en minúsculas y sin tildes.
    private static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim().ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");
    }
}