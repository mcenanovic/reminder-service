using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReminderService.Api.Converters;

public class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public const string OutputFormat = "yyyy-MM-dd'T'HH:mm:ssZ";

    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd"
    ];

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString()!;
        return DateTimeOffset.TryParseExact(str, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) 
            ? result 
            : throw new JsonException($"Unable to parse '{str}' as a DateTimeOffset in ISO 8601 format.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ssZ"));
    }
}
