using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowzy.Service.Contracts;

public sealed class DifficultyLevelJsonConverter : JsonConverter<string>
{
    private static readonly string[] Values = ["BEGINNER", "INTERMEDIATE", "ADVANCED"];
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var ordinal) && ordinal is >= 0 and < 3)
            return Values[ordinal];
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString()!.Trim();
            if (Values.Contains(value, StringComparer.Ordinal)) return value;
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ordinal) && ordinal is >= 0 and < 3)
                return Values[ordinal];
        }
        throw new JsonException("Invalid DifficultyLevel value");
    }
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
