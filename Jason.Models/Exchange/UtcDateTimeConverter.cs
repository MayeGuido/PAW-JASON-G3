using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jason.Models.Exchange;

// El formato común exige fechas ISO 8601 en UTC terminando en Z.
// Una fecha que viene de SQL (DATETIME) llega con Kind = Unspecified y, sin este convertidor,
// se escribiría sin la Z y el schema de los otros grupos la rechazaría.
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? throw new JsonException("La fecha viene vacía.");
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
        {
            throw new JsonException($"Fecha inválida: {text}");
        }
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(ToUtc(value).ToString(Format, CultureInfo.InvariantCulture));

    // Unspecified (lo que devuelve SQL) se interpreta como UTC
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
