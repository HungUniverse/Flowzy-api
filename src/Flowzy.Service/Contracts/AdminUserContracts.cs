using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Flowzy.Service.Contracts;

public sealed record StudentProfileInput(
    [Required(ErrorMessage = "Student code is required"), StringLength(50, ErrorMessage = "Student code must not exceed 50 characters")] string StudentCode,
    [Required(ErrorMessage = "Full name is required"), StringLength(255, ErrorMessage = "Full name must not exceed 255 characters")] string FullName,
    [StringLength(30, ErrorMessage = "Phone must not exceed 30 characters")] string? Phone,
    DateOnly? DateOfBirth,
    [property: JsonConverter(typeof(GenderJsonConverter))] string? Gender,
    string? Address,
    [StringLength(150, ErrorMessage = "Major must not exceed 150 characters")] string? Major,
    [StringLength(50, ErrorMessage = "Cohort must not exceed 50 characters")] string? Cohort,
    [StringLength(100, ErrorMessage = "Class name must not exceed 100 characters")] string? ClassName);
public sealed record MentorProfileInput(
    [Required(ErrorMessage = "Mentor code is required"), StringLength(50, ErrorMessage = "Mentor code must not exceed 50 characters")] string MentorCode,
    [Required(ErrorMessage = "Full name is required"), StringLength(255, ErrorMessage = "Full name must not exceed 255 characters")] string FullName,
    [StringLength(30, ErrorMessage = "Phone must not exceed 30 characters")] string? Phone,
    [StringLength(150, ErrorMessage = "Job title must not exceed 150 characters")] string? JobTitle,
    [StringLength(150, ErrorMessage = "Company must not exceed 150 characters")] string? Company,
    string? Expertise,
    [Range(0, int.MaxValue, ErrorMessage = "Years of experience must be at least 0")] int? YearsOfExperience,
    [StringLength(500, ErrorMessage = "LinkedIn URL must not exceed 500 characters")] string? LinkedinUrl);
public sealed record InstructorProfileInput(
    [Required(ErrorMessage = "Instructor code is required"), StringLength(50, ErrorMessage = "Instructor code must not exceed 50 characters")] string InstructorCode,
    [Required(ErrorMessage = "Full name is required"), StringLength(255, ErrorMessage = "Full name must not exceed 255 characters")] string FullName,
    [StringLength(30, ErrorMessage = "Phone must not exceed 30 characters")] string? Phone,
    [StringLength(150, ErrorMessage = "Department must not exceed 150 characters")] string? Department,
    string? Expertise);
public sealed record CreateAdminUserRequest([Required, EmailAddress] string Email, [Required] string Role,
    [Required, MinLength(6)] string InitialPassword, StudentProfileInput? StudentProfile, MentorProfileInput? MentorProfile,
    InstructorProfileInput? InstructorProfile);
public sealed record UpdateAdminUserRequest([Required, EmailAddress] string Email, [Required] string Status,
    bool MustChangePassword, StudentProfileInput? StudentProfile, MentorProfileInput? MentorProfile, InstructorProfileInput? InstructorProfile);
public sealed record ResetUserPasswordRequest([Required, MinLength(6)] string NewPassword);
public sealed record AdminChangePasswordRequest([Required, EmailAddress] string Email, [Required, MinLength(6)] string NewPassword);
public sealed record AdminUserSummaryResponse(long Id, string Email, string Role, string Status, bool MustChangePassword,
    string? FullName, string? Code, DateTime CreatedAt, DateTime? LastLoginAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StudentGroupMembershipResponse>? GroupMemberships);
public sealed record AdminUserDetailResponse(long Id, string Email, string Role, string Status, bool MustChangePassword,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? LastLoginAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] StudentProfileResponse? StudentProfile,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MentorProfileResponse? MentorProfile,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] InstructorProfileResponse? InstructorProfile,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StudentGroupMembershipResponse>? GroupMemberships);
