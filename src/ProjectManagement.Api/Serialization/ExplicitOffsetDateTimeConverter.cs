using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagement.Api.Serialization;

public class ExplicitOffsetDateTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("A timestamp with Z or an explicit UTC offset is required.");
        var text = reader.GetString()!;
        var hasOffset = text.EndsWith('Z') || (text.Length >= 6
            && (text[^6] == '+' || text[^6] == '-') && text[^3] == ':');
        if (!hasOffset || !reader.TryGetDateTimeOffset(out var value))
            throw new JsonException("A valid ISO 8601 timestamp with Z or an explicit UTC offset is required.");
        return value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
}
