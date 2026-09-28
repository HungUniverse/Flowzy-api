using System.ComponentModel.DataAnnotations;
namespace Flowzy.Service.Contracts;
public sealed record UpsertGroupGradeRequest(
    [Required(ErrorMessage = "Score is required"), Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Score must be non-negative")] decimal? Score,
    [MaxLength(5000, ErrorMessage = "Feedback must be at most 5000 characters")] string? Feedback);
public sealed record ContributionItem([Required(ErrorMessage = "Student ID is required")] long? StudentId,
    [Required(ErrorMessage = "Contribution percent is required"), ContributionPercent] decimal? ContributionPercent);
public sealed record UpsertContributionsRequest([Required(ErrorMessage = "Contribution items are required"), MinLength(1, ErrorMessage = "Contribution items are required")] IReadOnlyList<ContributionItem> Items);
public sealed record ContributionAgreementRequest(
    [Required(ErrorMessage = "must not be null")] [property: System.Text.Json.Serialization.JsonConverter(typeof(ContributionDecisionConverter))] string Decision,
    [MaxLength(1000, ErrorMessage = "size must be between 0 and 1000")] string? Reason);
public sealed class ContributionPercentAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) => value is decimal percent
        ? percent < 0 ? new("Contribution percent must be at least 0") : percent > 100 ? new("Contribution percent must be at most 100") : ValidationResult.Success : ValidationResult.Success;
}
public sealed class ContributionDecisionConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string? Read(ref System.Text.Json.Utf8JsonReader reader, Type type, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.Number && reader.TryGetInt32(out var n) && n is 0 or 1) return n == 0 ? "AGREE" : "REQUEST_CHANGES";
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            var value = reader.GetString()!.Trim();
            if (value is "AGREE" or "REQUEST_CHANGES") return value;
            if (value is "0" or "1") return value == "0" ? "AGREE" : "REQUEST_CHANGES";
        }
        throw new System.Text.Json.JsonException("Invalid ContributionDecision value");
    }
    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options) => writer.WriteStringValue(value);
}

public sealed record MatrixGrade(long Id, long MilestoneId, long GroupId, decimal Score, decimal MaxScoreSnapshot, int WeightSnapshot, string? Feedback, long? InstructorId, DateTime GradedAt, bool ContributionsComplete, bool GradeComplete);
public sealed record MatrixColumn(long MilestoneId, string Title, int Weight, decimal MaxScore, MatrixGrade? GroupGrade, bool Graded, bool ContributionsComplete, string ContributionAgreementStatus, int ContributionRevision, int ApprovedCount, int RequiredCount, bool GradeComplete);
public sealed record MatrixScore(long MilestoneId, decimal? ContributionPercent, decimal? CalculatedScore, string? AgreementDecision, string? AgreementReason, DateTime? AgreementRespondedAt, bool Complete);
public sealed record MatrixMember(long StudentId, string StudentCode, string StudentName, decimal TotalScore, bool Complete, IReadOnlyList<MatrixScore> MilestoneScores);
public sealed record GroupGradeMatrix(long GroupId, string GroupName, string GroupNo, string Term, string CourseCode, IReadOnlyList<MatrixColumn> Milestones, IReadOnlyList<MatrixMember> Members, bool Complete);
