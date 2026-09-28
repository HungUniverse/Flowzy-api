using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Admin;

public sealed class AdminUserService(IAdminUserRepository users, IRefreshTokenRepository refreshTokens) : IAdminUserService
{
    private static readonly HashSet<string> Roles = ["ADMIN", "STUDENT", "MENTOR", "INSTRUCTOR"];
    private static readonly HashSet<string> Statuses = ["ACTIVE", "INACTIVE", "LOCKED"];

    public async Task<PageResponse<AdminUserSummaryResponse>> SearchAsync(int page, int size, string? search, string? role, string? status, CancellationToken ct)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        var result = await users.SearchAsync(page, size, search, role, status, ct);
        return PageResponse<AdminUserSummaryResponse>.Create(result.Items.Select(MapSummary).ToList(), page, size, result.Total);
    }

    public async Task<AdminUserDetailResponse> GetAsync(long id, CancellationToken ct) => MapDetail(await GetAccount(id, ct));

    public async Task<AdminUserDetailResponse> CreateAsync(CreateAdminUserRequest request, CancellationToken ct)
    {
        var role = request.Role.ToUpperInvariant(); ValidateRole(role); ValidateProfile(role, request.StudentProfile, request.MentorProfile, request.InstructorProfile);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email, ct) is not null) throw new BadRequestException($"Email is already in use: {email}");
        var account = new Account { Email = email, Role = role, Status = "ACTIVE", MustChangePassword = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.InitialPassword), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await ApplyProfile(account, role, request.StudentProfile, request.MentorProfile, request.InstructorProfile, null, ct);
        await users.AddAsync(account, ct); return MapDetail(account);
    }

    public async Task<AdminUserDetailResponse> UpdateAsync(long id, UpdateAdminUserRequest request, string currentEmail, CancellationToken ct)
    {
        var account = await GetAccount(id, ct); var email = request.Email.Trim().ToLowerInvariant(); var status = request.Status.ToUpperInvariant();
        if (!Statuses.Contains(status)) throw new BadRequestException("Invalid account status");
        var sameEmail = await users.FindByEmailAsync(email, ct);
        if (sameEmail is not null && sameEmail.Id != id) throw new BadRequestException($"Email is already in use: {email}");
        if (account.Email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase) && status != "ACTIVE")
            throw new BadRequestException($"You cannot delete/disable your own currently authenticated account: {currentEmail}");
        ValidateProfile(account.Role, request.StudentProfile, request.MentorProfile, request.InstructorProfile);
        account.Email = email; account.Status = status; account.MustChangePassword = request.MustChangePassword; account.UpdatedAt = DateTime.UtcNow;
        await ApplyProfile(account, account.Role, request.StudentProfile, request.MentorProfile, request.InstructorProfile, id, ct);
        if (status == "INACTIVE") await refreshTokens.DeleteForAccountAsync(id, ct);
        await users.SaveAsync(ct); return MapDetail(account);
    }

    public async Task DeleteAsync(long id, string currentEmail, CancellationToken ct)
    {
        var a = await GetAccount(id, ct);
        if (a.Email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase)) throw new BadRequestException($"You cannot delete/disable your own currently authenticated account: {currentEmail}");
        a.Status = "INACTIVE"; if (a.Student is not null) a.Student.Status = "INACTIVE"; if (a.Mentor is not null) a.Mentor.Status = "INACTIVE";
        if (a.Instructor is not null) a.Instructor.Status = "INACTIVE"; await refreshTokens.DeleteForAccountAsync(id, ct); await users.SaveAsync(ct);
    }

    public async Task ResetPasswordAsync(long id, string password, CancellationToken ct)
    { var a = await GetAccount(id, ct); a.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password); a.MustChangePassword = true; await refreshTokens.DeleteForAccountAsync(id, ct); await users.SaveAsync(ct); }
    public async Task ChangePasswordAsync(string email, string password, CancellationToken ct)
    { var a = await users.FindByEmailAsync(email.Trim(), ct) ?? throw new NotFoundException($"Account not found with email: {email}"); a.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password); a.MustChangePassword = false; await refreshTokens.DeleteForAccountAsync(a.Id, ct); await users.SaveAsync(ct); }

    private async Task<Account> GetAccount(long id, CancellationToken ct) => await users.FindAsync(id, ct) ?? throw new NotFoundException($"Account not found with ID: {id}");
    private static void ValidateRole(string role) { if (!Roles.Contains(role)) throw new BadRequestException("Invalid role"); }
    private static void ValidateProfile(string role, StudentProfileInput? student, MentorProfileInput? mentor, InstructorProfileInput? instructor)
    {
        if (role == "STUDENT")
        {
            if (student is null) throw new BadRequestException("Student profile details are required for STUDENT role");
            if (mentor is not null || instructor is not null) throw new BadRequestException("Mentor or Instructor profiles are not allowed for STUDENT role");
        }
        else if (role == "MENTOR")
        {
            if (mentor is null) throw new BadRequestException("Mentor profile details are required for MENTOR role");
            if (student is not null || instructor is not null) throw new BadRequestException("Student or Instructor profiles are not allowed for MENTOR role");
        }
        else if (role == "INSTRUCTOR")
        {
            if (instructor is null) throw new BadRequestException("Instructor profile details are required for INSTRUCTOR role");
            if (student is not null || mentor is not null) throw new BadRequestException("Student or Mentor profiles are not allowed for INSTRUCTOR role");
        }
        else if (student is not null || mentor is not null || instructor is not null)
            throw new BadRequestException("Profiles are not allowed for ADMIN role");
    }

    private async Task ApplyProfile(Account a, string role, StudentProfileInput? s, MentorProfileInput? m, InstructorProfileInput? i, long? except, CancellationToken ct)
    {
        if (role == "STUDENT" && s is not null)
        {
            if (await users.ProfileCodeExistsAsync(role, s.StudentCode, except, ct)) throw new BadRequestException($"Student code already exists: {s.StudentCode}");
            a.Student ??= new Student { Account = a, CreatedAt = DateTime.UtcNow };
            var x = a.Student; x.StudentCode = s.StudentCode; x.FullName = s.FullName; x.Phone = s.Phone; x.DateOfBirth = s.DateOfBirth;
            x.Gender = s.Gender; x.Address = s.Address; x.Major = s.Major; x.Cohort = s.Cohort; x.ClassName = s.ClassName; x.Status = a.Status; x.UpdatedAt = DateTime.UtcNow;
        }
        if (role == "MENTOR" && m is not null)
        {
            if (await users.ProfileCodeExistsAsync(role, m.MentorCode, except, ct)) throw new BadRequestException($"Mentor code already exists: {m.MentorCode}");
            a.Mentor ??= new Mentor { Account = a, CreatedAt = DateTime.UtcNow };
            var x = a.Mentor; x.MentorCode = m.MentorCode; x.FullName = m.FullName; x.Phone = m.Phone; x.JobTitle = m.JobTitle; x.Company = m.Company;
            x.Expertise = m.Expertise; x.YearsOfExperience = m.YearsOfExperience; x.LinkedinUrl = m.LinkedinUrl; x.Status = a.Status; x.UpdatedAt = DateTime.UtcNow;
        }
        if (role == "INSTRUCTOR" && i is not null)
        {
            if (await users.ProfileCodeExistsAsync(role, i.InstructorCode, except, ct)) throw new BadRequestException($"Instructor code already exists: {i.InstructorCode}");
            a.Instructor ??= new Instructor { Account = a, CreatedAt = DateTime.UtcNow };
            var x = a.Instructor; x.InstructorCode = i.InstructorCode; x.FullName = i.FullName; x.Phone = i.Phone; x.Department = i.Department;
            x.Expertise = i.Expertise; x.Status = a.Status; x.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static IReadOnlyList<StudentGroupMembershipResponse>? Memberships(Account a) => a.Student?.StudentGroupMembers.Select(x =>
        new StudentGroupMembershipResponse(x.GroupId, x.Group.Term, x.Group.CourseCode, x.Group.GroupNo, x.Group.Name, x.Group.ProjectName, x.MemberRole, x.JoinedAt)).ToList();
    private static AdminUserSummaryResponse MapSummary(Account a) => new(a.Id, a.Email, a.Role, a.Status, a.MustChangePassword,
        a.Student?.FullName ?? a.Mentor?.FullName ?? a.Instructor?.FullName, a.Student?.StudentCode ?? a.Mentor?.MentorCode ?? a.Instructor?.InstructorCode,
        a.CreatedAt, a.LastLoginAt, Memberships(a));
    private static AdminUserDetailResponse MapDetail(Account a) => new(a.Id, a.Email, a.Role, a.Status, a.MustChangePassword, a.CreatedAt, a.UpdatedAt, a.LastLoginAt,
        a.Student is null ? null : new(a.Student.Id, a.Student.StudentCode, a.Student.FullName, a.Email, a.Student.Phone, a.Student.DateOfBirth, a.Student.Gender, a.Student.Address, a.Student.Major, a.Student.Cohort, a.Student.ClassName, a.Student.Status),
        a.Mentor is null ? null : new(a.Mentor.Id, a.Mentor.MentorCode, a.Mentor.FullName, a.Email, a.Mentor.Phone, a.Mentor.JobTitle, a.Mentor.Company, a.Mentor.Expertise, a.Mentor.YearsOfExperience, a.Mentor.LinkedinUrl, a.Mentor.Status),
        a.Instructor is null ? null : new(a.Instructor.Id, a.Instructor.InstructorCode, a.Instructor.FullName, a.Email, a.Instructor.Phone, a.Instructor.Department, a.Instructor.Expertise, a.Instructor.Status), Memberships(a));
}
