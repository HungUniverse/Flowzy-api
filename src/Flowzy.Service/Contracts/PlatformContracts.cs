using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Flowzy.Service.Contracts;

public sealed record PageResponse<T>(IReadOnlyList<T> Content, int Page, int Number, int Size, int NumberOfElements,
    long TotalElements, int TotalPages, bool HasNext, bool HasPrevious)
{
    public static PageResponse<T> Create(IReadOnlyList<T> content, int page, int size, long total) =>
        new(content, page, page, size, content.Count, total, size == 0 ? 0 : (int)Math.Ceiling(total / (double)size),
            (page + 1L) * size < total, page > 0);
}

public sealed record AcademicTermResponse(long Id, string Code, string Status, DateTime? ClosedAt, string? ClosedByEmail,
    long GroupCount, long TotalExpectedFeedbacks, long TotalSubmittedFeedbacks);
public sealed record ProblemDomainResponse(long Id, string Code, string Name, string? Description, string? MacroDomain,
    string? SubDomain, string? TypicalExamples, string? PrimaryDiscipline, string? SupportingDisciplines,
    string? BestSources, string? StudentCapabilities, string? PotentialOutputs, string? Notes, string Status,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record ProblemCriterionResponse(long Id, string Code, string Category, string Question, string? Suggestion,
    int MaxScore, int DisplayOrder, bool Active, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record ProblemSummaryResponse(long Id, string? Code, string Title, string? DomainCode, string? DomainName,
    string DifficultyLevel, string SourceType, string Status, string? StrategicTheme, string? ResearchArea,
    long? ProposedByGroupId, string? ProposedByGroupNo, string? ProposedByGroupName);
public sealed record DomainInfo(long Id, string Code, string Name);
public sealed record GroupInfo(long Id, string GroupNo, string? Name);
public sealed record StudentInfo(long Id, string StudentCode, string FullName);
public sealed record ReviewerInfo(long Id, string Email);
public sealed record ProblemDetailResponse(long Id, string? Code, string Title, string Statement, string? StrategicTheme,
    string? ResearchArea, string DifficultyLevel, string? ExpectedOutput, string? OwnerLab, string? SuggestedCourses,
    string? DriveFolderLink, string SourceType, string Status, DomainInfo? Domain, GroupInfo? ProposedByGroup,
    StudentInfo? ProposedByStudent, string? ReviewComment, ReviewerInfo? ReviewedBy, DateTime? ReviewedAt,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record RecruitmentRoleResponse(string Code, string Category, string DisplayNameVi, string DisplayNameEn);
public sealed record NotificationActionResponse(string Key, IReadOnlyDictionary<string, string> Params);
public sealed record NotificationResponse(long Id, string Type, string Title, string Body, string? ActionUrl,
    string? EntityType, string? EntityId, string? Payload, NotificationActionResponse Action, bool Read,
    DateTime? ReadAt, DateTime CreatedAt);

public sealed record ChangeOwnPasswordRequest(
    [Required(ErrorMessage = "Current password is required")] string CurrentPassword,
    [Required(ErrorMessage = "New password is required"), MinLength(6, ErrorMessage = "New password must be at least 6 characters")] string NewPassword);
public sealed record UpdateSelfProfileRequest(
    [StringLength(255, ErrorMessage = "Full name must not exceed 255 characters")] string? FullName,
    [StringLength(30, ErrorMessage = "Phone must not exceed 30 characters")] string? Phone,
    DateOnly? DateOfBirth,
    [property: JsonConverter(typeof(GenderJsonConverter))] string? Gender,
    string? Address,
    [StringLength(150, ErrorMessage = "Major must not exceed 150 characters")] string? Major,
    [StringLength(50, ErrorMessage = "Cohort must not exceed 50 characters")] string? Cohort,
    [StringLength(100, ErrorMessage = "Class name must not exceed 100 characters")] string? ClassName,
    [StringLength(150, ErrorMessage = "Job title must not exceed 150 characters")] string? JobTitle,
    [StringLength(150, ErrorMessage = "Company must not exceed 150 characters")] string? Company,
    string? Expertise,
    [Range(0, int.MaxValue, ErrorMessage = "Years of experience must be at least 0")] int? YearsOfExperience,
    [StringLength(500, ErrorMessage = "LinkedIn URL must not exceed 500 characters")] string? LinkedinUrl,
    [StringLength(150, ErrorMessage = "Department must not exceed 150 characters")] string? Department);
public sealed record StudentProfileResponse(long Id, string StudentCode, string FullName, string Email, string? Phone,
    DateOnly? DateOfBirth, string? Gender, string? Address, string? Major, string? Cohort, string? ClassName, string Status);
public sealed record MentorProfileResponse(long Id, string MentorCode, string FullName, string Email, string? Phone,
    string? JobTitle, string? Company, string? Expertise, int? YearsOfExperience, string? LinkedinUrl, string Status);
public sealed record InstructorProfileResponse(long Id, string InstructorCode, string FullName, string Email, string? Phone,
    string? Department, string? Expertise, string Status);
public sealed record StudentGroupMembershipResponse(long GroupId, string Term, string CourseCode, string GroupNo,
    string? Name, string? ProjectName, string Role, DateTime JoinedAt);
public sealed record SelfProfileResponse(long Id, string Email, string Role, string Status, bool MustChangePassword,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? LastLoginAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] StudentProfileResponse? StudentProfile,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MentorProfileResponse? MentorProfile,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] InstructorProfileResponse? InstructorProfile,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StudentGroupMembershipResponse>? GroupMemberships);
