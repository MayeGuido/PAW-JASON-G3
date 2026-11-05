using Jason.Models.Exchange;

namespace Jason.API.Parsers;

// TODO: extraer títulos y enlaces de una página HTML.
// Acordar con el grupo qué va en Data (por ejemplo { "html": "..." }).
public class HtmlContentParser : IContentParser
{
    public string Format => "html";

    public IReadOnlyList<ExportItem> Parse(string content, ExportSource source)
        => throw new NotImplementedException("Parser HTML pendiente.");
}
