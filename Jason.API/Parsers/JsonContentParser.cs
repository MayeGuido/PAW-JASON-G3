using System.Text.Json;
using Jason.API.Exceptions;
using Jason.Models.Exchange;

namespace Jason.API.Parsers;
//aca pasa lo mismo que con el parse de xml, este parser es el encargado de parsear el contenido de la fuente, en este caso json,
//y devolver una lista de ExportItem que es lo que se va a devolver al usuario final
public class JsonContentParser : IContentParser
{
    public string Format => "json";

    public IReadOnlyList<ExportItem> Parse(string content, ExportSource source)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);

            List<JsonElement> jsonItems = FindItems(document.RootElement);
            List<ExportItem> result = new();
            HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);
            DateTime capturedAt = DateTime.UtcNow;
            int position = 1;

            foreach (JsonElement jsonItem in jsonItems)
            {
                string? title = ContentParserUtilities.ReadString(jsonItem,
                    "title", "name", "headline", "question", "text");
                string? url = ContentParserUtilities.ReadString(jsonItem,
                    "url", "link", "html_url", "web_url", "uri");
                string? thumbnail = ContentParserUtilities.ReadString(jsonItem,
                    "thumbnail", "thumbnail_url", "image", "image_url", "urlToImage", "banner_image");
                string? originalId = ContentParserUtilities.ReadString(jsonItem,
                    "id", "uuid", "guid", "slug", "key", "objectID");

                if (string.IsNullOrWhiteSpace(originalId))
                    originalId = ContentParserUtilities.StableId(jsonItem.GetRawText());
                if (string.IsNullOrWhiteSpace(title))
                    title = source.Name + " " + position;

                ExportItem item = new()
                {
                    Id = ContentParserUtilities.UniqueId(originalId, usedIds),
                    Title = title.Trim(),
                    MediaType = ContentParserUtilities.NormalizeMediaType(
                        ContentParserUtilities.ReadString(jsonItem, "mediaType", "media_type", "type", "kind"),
                        thumbnail ?? url),
                    CreatedAt = ContentParserUtilities.ReadDate(jsonItem, capturedAt,
                        "createdAt", "created_at", "publishedAt", "published_at", "published",
                        "date", "time", "datetime", "publication_date", "first_publish_year"),
                    Category = ContentParserUtilities.NormalizeCategory(
                        ContentParserUtilities.ReadString(jsonItem, "category", "section", "topic"), source),
                    Url = url,
                    Thumbnail = thumbnail,
                    Description = ContentParserUtilities.ReadString(jsonItem,
                        "description", "summary", "abstract", "explanation", "content"),
                    Data = ContentParserUtilities.AsObject(jsonItem)
                };

                result.Add(item);
                position++;
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw ApiException.BadRequest(
                "La fuente " + source.Name + " devolvió JSON inválido: " + exception.Message);
        }
    }

    private static List<JsonElement> FindItems(JsonElement root)
    {
        List<JsonElement> result = new();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in root.EnumerateArray())
                result.Add(item.Clone());
            return result;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            string[] possibleLists = { "results", "items", "articles", "entries", "features", "docs", "posts", "data" };

            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (possibleLists.Contains(property.Name, StringComparer.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in property.Value.EnumerateArray())
                        result.Add(item.Clone());
                    return result;
                }
            }
        }

        // Si no existe una lista, la respuesta completa se toma como una sola ficha.
        result.Add(root.Clone());
        return result;
    }
}