using Jason.Models.Exchange;

namespace Jason.API.Parsers;

// TODO: convertir la respuesta JSON de la API en ExportItem.
// Reglas: fecha en UTC (si la API no trae fecha, usar la de captura), id único dentro de la fuente,
// y la respuesta original completa en Data (como objeto, nunca como texto).
public class JsonContentParser : IContentParser
{
    public string Format => "json";

    public IReadOnlyList<ExportItem> Parse(string content, ExportSource source)
        => throw new NotImplementedException("Parser JSON pendiente.");
}
