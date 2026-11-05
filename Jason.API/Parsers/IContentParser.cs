using Jason.Models.Exchange;

namespace Jason.API.Parsers;

// Patrón Strategy: un parser por formato. Todos devuelven los mismos ExportItem del formato común (patrón Adapter).
public interface IContentParser
{
    // json, xml o html
    string Format { get; }

    IReadOnlyList<ExportItem> Parse(string content, ExportSource source);
}
