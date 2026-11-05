using Jason.Models.Exchange;

namespace Jason.API.Parsers;

// TODO: RSS y Atom. Cada <item> o <entry> se convierte en un ExportItem
// y su contenido XML se pasa a objeto dentro de Data.
public class XmlContentParser : IContentParser
{
    public string Format => "xml";

    public IReadOnlyList<ExportItem> Parse(string content, ExportSource source)
        => throw new NotImplementedException("Parser XML pendiente.");
}
