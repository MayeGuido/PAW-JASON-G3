using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Jason.API.Exceptions;
using Jason.Models.Exchange;

namespace Jason.API.Parsers;


//Bueno en resumen, este parser es el encargado de parsear el contenido de la fuente en formato XML y devolver una lista de ExportItem.
public class XmlContentParser : IContentParser
{
    public string Format => "xml"; //aca se define el formato que este parser soporta, en este caso XML

    //definimos el metodo Parse que recibe el contenido de la fuente y el objeto ExportSource, y devuelve una lista de ExportItem
    public IReadOnlyList<ExportItem> Parse(string content, ExportSource source)
    {
        //aca se define un try catch para capturar cualquier error que pueda ocurrir al parsear el contenido de la fuente
        //tambien definimos los metodos privados que se usan para parsear el contenido de la fuente, como ReadXml, FindItems, FindLink, FindCategory, ChildValue, FindChild y ElementToDictionary
        try
        {
            //aca abajo se definen varias variables que se van a usar para parsear el contenido de la fuente
            XDocument document = ReadXml(content);
            List<XElement> xmlItems = FindItems(document);
            List<ExportItem> result = new();
            HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);
            DateTime capturedAt = DateTime.UtcNow;
            int position = 1;


            //este foreach recorre cada elemento XML que se encuentra en la lista de xmlItems y extrae la información relevante para crear un objeto ExportItem
            foreach (XElement xmlItem in xmlItems) 
            {
                string? title = ChildValue(xmlItem, "title");
                string? url = FindLink(xmlItem);
                string? originalId = ChildValue(xmlItem, "guid", "id");
                string? description = ContentParserUtilities.PlainText(
                    ChildValue(xmlItem, "description", "summary", "content"));
                string? category = FindCategory(xmlItem);
                XElement? media = FindChild(xmlItem, "enclosure", "content");
                string? mediaUrl = media?.Attribute("url")?.Value;
                string? mediaType = media?.Attribute("type")?.Value;

                if (string.IsNullOrWhiteSpace(originalId))
                    originalId = url ?? ContentParserUtilities.StableId(xmlItem.ToString());
                if (string.IsNullOrWhiteSpace(title))
                    title = source.Name + " " + position;

                ExportItem item = new()
                {
                    Id = ContentParserUtilities.UniqueId(originalId, usedIds),
                    Title = title.Trim(),
                    MediaType = ContentParserUtilities.NormalizeMediaType(mediaType, mediaUrl ?? url),
                    CreatedAt = ContentParserUtilities.ParseDate(
                        ChildValue(xmlItem, "pubDate", "published", "updated", "date"), capturedAt),
                    Category = ContentParserUtilities.NormalizeCategory(category, source),
                    Url = url,
                    Thumbnail = mediaUrl,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    Data = JsonSerializer.SerializeToElement(ElementToDictionary(xmlItem))
                };

                result.Add(item);
                position++;
            }

            return result;
        }
        catch (XmlException exception)
        {
            throw ApiException.BadRequest(
                "La fuente " + source.Name + " devolvió XML inválido: " + exception.Message);
        }
    }

    private static XDocument ReadXml(string content)
    {
        // Se prohíben DTD externos para que el XML no pueda leer archivos del equipo.
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        using StringReader textReader = new(content);
        using XmlReader xmlReader = XmlReader.Create(textReader, settings);
        return XDocument.Load(xmlReader);
    }

    private static List<XElement> FindItems(XDocument document)
    {
        List<XElement> result = new();

        foreach (XElement element in document.Descendants())
        {
            if (element.Name.LocalName == "item" || element.Name.LocalName == "entry")
                result.Add(element);
        }

        if (result.Count == 0 && document.Root != null)
            result.Add(document.Root);

        return result;
    }

    private static string? FindLink(XElement item)
    {
        XElement? link = FindChild(item, "link");
        if (link == null) return null;

        // Atom usa href; RSS normalmente guarda el enlace como texto.
        return link.Attribute("href")?.Value ?? link.Value;
    }

    private static string? FindCategory(XElement item)
    {
        XElement? category = FindChild(item, "category");
        if (category == null) return null;
        return category.Attribute("term")?.Value ?? category.Value;
    }

    private static string? ChildValue(XElement item, params string[] names)
    {
        XElement? child = FindChild(item, names);
        return child?.Value;
    }

    private static XElement? FindChild(XElement item, params string[] names)
    {
        foreach (XElement child in item.Elements())
        {
            if (names.Contains(child.Name.LocalName, StringComparer.OrdinalIgnoreCase))
                return child;
        }
        return null;
    }

    private static Dictionary<string, object?> ElementToDictionary(XElement element)
    {
        Dictionary<string, object?> result = new();

        if (element.HasAttributes)
        {
            Dictionary<string, string> attributes = new();
            foreach (XAttribute attribute in element.Attributes())
                attributes[attribute.Name.LocalName] = attribute.Value;
            result["attributes"] = attributes;
        }

        foreach (XElement child in element.Elements())
        {
            object value = child.HasElements
                ? ElementToDictionary(child)
                : child.Value;

            // Si una etiqueta se repite, se guarda como una lista.
            if (result.TryGetValue(child.Name.LocalName, out object? previousValue))
            {
                List<object?> values;
                if (previousValue is List<object?> existingList)
                {
                    values = existingList;
                }
                else
                {
                    values = new List<object?> { previousValue };
                    result[child.Name.LocalName] = values;
                }
                values.Add(value);
            }
            else
            {
                result[child.Name.LocalName] = value;
            }
        }

        if (!element.HasElements && !string.IsNullOrWhiteSpace(element.Value))
            result["value"] = element.Value;

        return result;
    }
}