using System.Text.Json;
using System.Text.Json.Serialization;

namespace OperationsMonitoringService.Contracts;

public sealed class RecordedAtJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("recordedAt must be an ISO 8601 date-time with an explicit offset.");
        var text = reader.GetString()!;
        var hasOffset = text.EndsWith('Z') ||
            (text.Length >= 6 && text[^6] is '+' or '-' && text[^3] == ':');
        if (!hasOffset || !reader.TryGetDateTimeOffset(out var value))
            throw new JsonException("recordedAt must be an ISO 8601 date-time with an explicit offset.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
