namespace Flowzy.Service.Contracts;

public sealed record MilestoneGradeResponse(
    long Id,
    long SubmissionId,
    decimal Score,
    decimal MaxScore,
    string? Feedback,
    long? InstructorId,
    DateTime GradedAt);

public sealed record AverageGradeResponse(decimal AverageGrade, decimal Average);
