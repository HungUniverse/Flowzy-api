namespace Flowzy.Service.Contracts;

public enum SubmissionStatus
{
    SUBMITTED,
    RESUBMITTED,
    GRADED
}

public sealed record MilestoneSubmissionResponse(long Id, long MilestoneId, long GroupId, string? SubmittedBy,
    string? FileUrl, string? Comments, DateTime SubmittedAt, bool Late, string Status, long Version,
    decimal? Score, decimal? MaxScore, string? Feedback, DateTime? GradedAt, DateTime CreatedAt, DateTime UpdatedAt);
