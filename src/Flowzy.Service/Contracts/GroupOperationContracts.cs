using System.ComponentModel.DataAnnotations;

namespace Flowzy.Service.Contracts;

public sealed record RecruitmentNeedRequest([Required] string Role, int Quantity);
public sealed record CreateGroupRequest([Required] string Term,[Required] string CourseCode,[Required] string Name,
    string? ProjectName,string? IdeaDescription,string? ResearchDomain,decimal? RequiredGpa,decimal? TargetGrade,
    IReadOnlyList<RecruitmentNeedRequest>? RecruitmentNeeds);
public sealed record UpdateGroupRequest(string? Name,string? ProjectName,string? IdeaDescription,string? ResearchDomain,
    decimal? RequiredGpa,decimal? TargetGrade,IReadOnlyList<RecruitmentNeedRequest>? RecruitmentNeeds);
public sealed record UpdateGroupCriteriaRequest(decimal? RequiredGpa,decimal? TargetGrade,IReadOnlyList<RecruitmentNeedRequest>? RecruitmentNeeds);
public sealed record TransferLeaderRequest([Required] long StudentId);
public sealed record AssignInstructorRequest([Required] long InstructorId);
public sealed record AssignMentorRequest([Required] long MentorId);
public sealed record UpdateGroupLockRequest([Required] bool IsLock);
public sealed record CreateInvitationRequest(
    [Required(ErrorMessage = "Student code or email is required")]
    [StringLength(255, ErrorMessage = "Student code or email must be at most 255 characters")] string StudentCodeOrEmail,
    [StringLength(500, ErrorMessage = "Message must be at most 500 characters")] string? Message);
public sealed record CreateJoinRequest([StringLength(500, ErrorMessage = "Message must be at most 500 characters")] string? Message);
public sealed record InvitationResponse(long Id,long GroupId,string GroupName,string GroupNo,string CourseCode,string Term,
    long InviterId,string InviterCode,string InviterName,long InviteeId,string InviteeCode,string InviteeName,
    long StudentId,string StudentCode,string StudentName,string Status,string? Message,DateTime CreatedAt,DateTime? RespondedAt);
public sealed record JoinRequestResponse(long Id,long GroupId,string GroupName,string GroupNo,string CourseCode,string Term,
    long StudentId,string StudentCode,string StudentName,string Status,string? Message,long? RespondedById,
    string? RespondedByCode,string? RespondedByName,DateTime? RespondedAt,DateTime CreatedAt,DateTime UpdatedAt);
public sealed record SelectProblemRequest([Required(ErrorMessage = "Problem ID is required")] long? ProblemId);
public sealed record ProposeGroupProblemRequest(
    [Required(ErrorMessage = "Title is required"), StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")] string Title,
    [Required(ErrorMessage = "Statement is required")] string Statement,
    [StringLength(255, ErrorMessage = "Strategic theme cannot exceed 255 characters")] string? StrategicTheme,
    [StringLength(255, ErrorMessage = "Research area cannot exceed 255 characters")] string? ResearchArea,
    [Required(ErrorMessage = "Difficulty level is required")]
    [property: System.Text.Json.Serialization.JsonConverter(typeof(DifficultyLevelJsonConverter))] string DifficultyLevel,
    string? ExpectedOutput,
    [Required(ErrorMessage = "Domain code is required")] string DomainCode);
