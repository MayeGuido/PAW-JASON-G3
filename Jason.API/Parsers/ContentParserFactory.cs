using Jason.API.Exceptions;

namespace Jason.API.Parsers;

// Patrón Factory: elige el parser según el formato. Agregar un formato nuevo es crear otro IContentParser
// y registrarlo en Program.cs, sin tocar el resto (abierto y cerrado).
public interface IContentParserFactory
{
    IContentParser Create(string format);

    // La tabla Sources del profesor no guarda el formato, así que se deduce del Content-Type o del contenido
    string DetectFormat(string content, string? contentType = null);
}

public class ContentParserFactory : IContentParserFactory
{
    private readonly IReadOnlyDictionary<string, IContentParser> _parsers;

    public ContentParserFactory(IEnumerable<IContentParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.Format, StringComparer.OrdinalIgnoreCase);
    }

    public IContentParser Create(string format) =>
        _parsers.TryGetValue(format, out var parser)
            ? parser
            : throw ApiException.BadRequest($"Formato no soportado: {format}.");

    public string DetectFormat(string content, string? contentType = null)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var type = contentType.ToLowerInvariant();
            if (type.Contains("json")) return "json";
            if (type.Contains("xml") || type.Contains("rss") || type.Contains("atom")) return "xml";
            if (type.Contains("html")) return "html";
        }

        var text = content.TrimStart();
        if (text.StartsWith('{') || text.StartsWith('[')) return "json";

        var start = text.Length > 500 ? text[..500] : text;
        if (start.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || start.Contains("<rss", StringComparison.OrdinalIgnoreCase)
            || start.Contains("<feed", StringComparison.OrdinalIgnoreCase)) return "xml";
        if (start.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)
            || start.Contains("<html", StringComparison.OrdinalIgnoreCase)) return "html";

        throw ApiException.BadRequest("No se pudo detectar el formato de la fuente (json, xml o html).");
    }
}
