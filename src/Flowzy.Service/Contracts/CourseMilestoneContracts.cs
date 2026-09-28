using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowzy.Service.Contracts;

public class TimelineWriteRequest
{
    [Required(ErrorMessage = "Title is required"), MaxLength(255, ErrorMessage = "Title must be at most 255 characters")]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [MilestoneWeight] public int? Weight { get; set; }
    public DateTime? DeadlineAt { get; set; }
    // Accept the legacy Java alias; only deadlineAt is serialized.
    [JsonPropertyName("dueDate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? DueDate { get => null; set => DeadlineAt = value; }
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Max score must be greater than 0")]
    public decimal? MaxScore { get; set; }
    [Range(typeof(long), "0", "9223372036854775807", ErrorMessage = "Position must be non-negative")]
    public long? Position { get; set; }
}
public sealed class CourseMilestoneRequest : TimelineWriteRequest
{
    [Required(ErrorMessage = "Term is required"), MaxLength(30, ErrorMessage = "Term must be at most 30 characters")]
    public string Term { get; set; } = null!;
    [Required(ErrorMessage = "Course code is required"), MaxLength(30, ErrorMessage = "Course code must be at most 30 characters")]
    public string CourseCode { get; set; } = null!;
}
public sealed class UpdateCourseMilestoneRequest : TimelineWriteRequest
{
    [JsonConverter(typeof(MilestoneStatusJsonConverter))] public string? Status { get; set; }
}
public sealed class MilestoneWeightAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) => value is int weight
        ? weight < 0 ? new("Weight must be non-negative") : weight > 100 ? new("Weight cannot exceed 100") : ValidationResult.Success
        : ValidationResult.Success;
}
public sealed class MilestoneStatusJsonConverter : JsonConverter<string>
{
    private static readonly string[] Values = ["ACTIVE", "CLOSED", "ARCHIVED", "INACTIVE"];
    public override string? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var index) && index is >= 0 and < 4) return Values[index];
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString()!.Trim();
            if (Values.Contains(value, StringComparer.Ordinal)) return value;
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out index) && index is >= 0 and < 4) return Values[index];
        }
        throw new JsonException("Invalid MilestoneStatus value");
    }
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
