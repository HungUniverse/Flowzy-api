using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Flowzy.Service.Contracts;

public sealed record ProblemDomainRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(255)] string Name,
    string? Description, string? MacroDomain, string? SubDomain, string? TypicalExamples,
    string? PrimaryDiscipline, string? SupportingDisciplines, string? BestSources,
    string? StudentCapabilities, string? PotentialOutputs, string? Notes, string? Status);

public sealed record UpdateProblemDomainRequest(string? Code, string? Name, string? Description,
    string? MacroDomain, string? SubDomain, string? TypicalExamples, string? PrimaryDiscipline,
    string? SupportingDisciplines, string? BestSources, string? StudentCapabilities,
    string? PotentialOutputs, string? Notes, string? Status);

public sealed record ProblemWriteRequest(string? DomainCode, [Required, MaxLength(255)] string Title,
    [Required] string Statement, string? Code, string? StrategicTheme, string? ResearchArea,
    [Required] string DifficultyLevel, string? ExpectedOutput, string? OwnerLab, string? SuggestedCourses,
    string? DriveFolderLink, string? Status);

public sealed record ProblemPatchRequest(string? DomainCode, string? Title, string? Statement, string? Code,
    string? StrategicTheme, string? ResearchArea, string? DifficultyLevel, string? ExpectedOutput,
    string? OwnerLab, string? SuggestedCourses, string? DriveFolderLink, string? Status);
public sealed record UpdateProblemStatusRequest([Required] string Status);
public sealed record CreateAcademicTermRequest([Required(ErrorMessage = "Term code is required"), MaxLength(30, ErrorMessage = "Term code must be at most 30 characters")] string Code);
public sealed record ArchiveTermStudentsResponse(long AlreadyInactiveStudents, long ArchivedStudents,
    long SkippedActiveInOpenTerm, long SkippedPendingFeedbackStudents, string TermCode);

public sealed record CourseMilestoneResponse(long Id, string Term, string CourseCode, string Title,
    string? Description, int? Weight, DateTime? DeadlineAt, decimal MaxScore, long Position,
    string Status, long? InstructorId, string? InstructorName);

public sealed record CreateTaskBoardRequest([Required(ErrorMessage = "Name is required"), MaxLength(255, ErrorMessage = "Name must be at most 255 characters")] string Name, string? Description);
public sealed record UpdateTaskBoardRequest(string? Name, string? Description, long? Position,
    bool? Archived, bool? DefaultBoard);
public sealed record TaskBoardResponse(long Id, long GroupId, string Name, string? Description,
    long Position, bool DefaultBoard, long? CreatedByStudentId, DateTime? ArchivedAt,
    DateTime CreatedAt, DateTime UpdatedAt);
