using System.ComponentModel.DataAnnotations;

namespace Flowzy.Service.Contracts;

public enum FeedbackStatus
{
    PENDING,
    SUBMITTED
}

public enum FeedbackTargetType
{
    MENTOR,
    INSTRUCTOR
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MinimumValueAttribute(int minimum) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is int number && number >= minimum;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MaximumValueAttribute(int maximum) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is int number && number <= maximum;
}

public sealed record SubmitFeedbackRequest(
    [Required(ErrorMessage = "Rating is required"), MinimumValue(1, ErrorMessage = "Rating must be at least 1"),
     MaximumValue(5, ErrorMessage = "Rating must be at most 5")] int? Rating,
    [StringLength(2000, ErrorMessage = "Comment must not exceed 2000 characters")] string? Comment);

public sealed record TermFeedbackResponse(long Id, long AcademicTermId, string AcademicTermCode,
    string AcademicTermStatus, long GroupId, string GroupName, long StudentId, string StudentName,
    string StudentCode, string TargetType, long? MentorId, string? MentorName, long? InstructorId,
    string? InstructorName, int? Rating, string? Comment, string Status, DateTime? SubmittedAt, long Version);

public sealed record ReceivedFeedbackEntry(long Id, int? Rating, string? Comment, DateTime? SubmittedAt,
    string? TermCode, string? CourseCode, long? GroupId, string? GroupName);

public sealed record FeedbackReceivedSummary(long TargetId, string TargetCode, string TargetName,
    string TargetType, string? Term, string? CourseCode, int TotalCount, double AverageRating,
    IReadOnlyDictionary<int, long> RatingDistribution, IReadOnlyList<ReceivedFeedbackEntry> Entries);

public sealed record FeedbackTermResponse(long Id, string Code, string Status);
public sealed record FeedbackGroupResponse(long Id, string Term, string CourseCode, string GroupNo, string Name,
    string? ProjectName);
public sealed record FeedbackStudentResponse(long Id, string StudentCode, string FullName, string? Email);
public sealed record FeedbackMentorResponse(long Id, string MentorCode, string FullName, string? Email);
public sealed record FeedbackInstructorResponse(long Id, string InstructorCode, string FullName, string? Email);
public sealed record AdminFeedbackResponse(long Id, FeedbackTermResponse AcademicTerm, FeedbackGroupResponse Group,
    FeedbackStudentResponse Student, string TargetType, FeedbackMentorResponse? Mentor,
    FeedbackInstructorResponse? Instructor, int? Rating, string? Comment, string Status, DateTime? SubmittedAt,
    DateTime CreatedAt, DateTime UpdatedAt);
