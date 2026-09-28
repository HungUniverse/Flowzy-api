using System.Text.Json;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Platform;

public sealed class PlatformService(IPlatformReadRepository repository, IRefreshTokenRepository refreshTokens) : IPlatformService
{
    public async Task<IReadOnlyList<AcademicTermResponse>> GetAvailableTermsAsync(CancellationToken ct)
    {
        var terms = await repository.GetOpenTermsAsync(ct);
        var result = new List<AcademicTermResponse>(terms.Count);
        foreach (var term in terms)
        {
            var counts = await repository.GetTermCountsAsync(term.Id, term.Code, ct);
            result.Add(new(term.Id, term.Code, term.Status, term.ClosedAt, term.ClosedByAccount?.Email,
                counts.Groups, counts.ExpectedFeedback, counts.SubmittedFeedback));
        }
        return result;
    }

    public async Task<IReadOnlyList<ProblemDomainResponse>> GetProblemDomainsAsync(string? search, string? status, CancellationToken ct) =>
        (await repository.GetProblemDomainsAsync(search, status, ct)).Select(x => new ProblemDomainResponse(x.Id, x.Code, x.Name,
            x.Description, x.MacroDomain, x.SubDomain, x.TypicalExamples, x.PrimaryDiscipline, x.SupportingDisciplines,
            x.BestSources, x.StudentCapabilities, x.PotentialOutputs, x.Notes, x.Status, x.CreatedAt, x.UpdatedAt)).ToList();

    public async Task<IReadOnlyList<ProblemCriterionResponse>> GetActiveCriteriaAsync(CancellationToken ct) =>
        (await repository.GetActiveCriteriaAsync(ct)).Select(x => new ProblemCriterionResponse(x.Id, x.Code, x.Category,
            x.Question, x.Suggestion, x.MaxScore, x.DisplayOrder, x.Active, x.CreatedAt, x.UpdatedAt)).ToList();

    public async Task<PageResponse<ProblemSummaryResponse>> SearchProblemsAsync(int page, int size, string? search,
        string? domainCode, string? difficulty, string? expectedOutput, string? sourceType, string? status, CancellationToken ct)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        var result = await repository.SearchProblemsAsync(page, size, search, domainCode, difficulty, expectedOutput, sourceType, status, ct);
        return PageResponse<ProblemSummaryResponse>.Create(result.Items.Select(x => new ProblemSummaryResponse(x.Id, x.Code, x.Title,
            x.Domain?.Code, x.Domain?.Name, x.DifficultyLevel, x.SourceType, x.Status, x.StrategicTheme, x.ResearchArea,
            x.ProposedByGroupId, x.ProposedByGroup?.GroupNo, x.ProposedByGroup?.Name)).ToList(), page, size, result.Total);
    }

    public async Task<ProblemDetailResponse> GetProblemAsync(long id, CancellationToken ct)
    {
        var x = await repository.FindProblemAsync(id, ct) ?? throw new NotFoundException($"Problem not found with id: {id}");
        return new(x.Id, x.Code, x.Title, x.Statement, x.StrategicTheme, x.ResearchArea, x.DifficultyLevel, x.ExpectedOutput,
            x.OwnerLab, x.SuggestedCourses, x.DriveFolderLink, x.SourceType, x.Status,
            x.Domain is null ? null : new DomainInfo(x.Domain.Id, x.Domain.Code, x.Domain.Name),
            x.ProposedByGroup is null ? null : new GroupInfo(x.ProposedByGroup.Id, x.ProposedByGroup.GroupNo, x.ProposedByGroup.Name),
            x.ProposedByStudent is null ? null : new StudentInfo(x.ProposedByStudent.Id, x.ProposedByStudent.StudentCode, x.ProposedByStudent.FullName),
            x.ReviewComment, x.ReviewedByAccount is null ? null : new ReviewerInfo(x.ReviewedByAccount.Id, x.ReviewedByAccount.Email),
            x.ReviewedAt, x.CreatedAt, x.UpdatedAt);
    }

    public async Task<PageResponse<NotificationResponse>> GetNotificationsAsync(string email, bool unreadOnly, int page, int size, CancellationToken ct)
    {
        var account = await ActiveAccount(email, ct);
        if (page < 0 || size <= 0) throw new BadRequestException("Invalid page or size parameters");
        size = Math.Min(size, 100);
        var result = await repository.GetNotificationsAsync(account.Id, unreadOnly, page, size, ct);
        return PageResponse<NotificationResponse>.Create(result.Items.Select(MapNotification).ToList(), page, size, result.Total);
    }

    public async Task<long> GetUnreadCountAsync(string email, CancellationToken ct) =>
        await repository.GetUnreadCountAsync((await ActiveAccount(email, ct)).Id, ct);

    public async Task<NotificationResponse> MarkNotificationReadAsync(long id, string email, CancellationToken ct)
    {
        var account = await ActiveAccount(email, ct);
        var notification = await repository.FindNotificationAsync(id, ct) ?? throw new NotFoundException($"Notification not found with id: {id}");
        if (notification.RecipientId != account.Id) throw new ForbiddenException("You are not authorized to access this notification");
        if (notification.ReadAt is null) { notification.ReadAt = DateTime.UtcNow; notification.UpdatedAt = DateTime.UtcNow; await repository.SaveAsync(ct); }
        return MapNotification(notification);
    }

    public async Task<long> MarkAllNotificationsReadAsync(string email, CancellationToken ct)
    {
        var account = await ActiveAccount(email, ct);
        await repository.MarkAllNotificationsReadAsync(account.Id, DateTime.UtcNow, ct);
        return await repository.GetUnreadCountAsync(account.Id, ct);
    }

    public async Task<SelfProfileResponse> GetProfileAsync(string email, CancellationToken ct) => MapProfile(await ActiveAccount(email, ct));

    public async Task<SelfProfileResponse> UpdateProfileAsync(string email, UpdateSelfProfileRequest request, CancellationToken ct)
    {
        var account = await ActiveAccount(email, ct);
        var name = request.FullName?.Trim();
        if (account.Role == "STUDENT" && account.Student is { } student)
        {
            ValidateName(request.FullName, name);
            if (name is not null) student.FullName = name; if (request.Phone is not null) student.Phone = Trim(request.Phone);
            if (request.DateOfBirth is not null) student.DateOfBirth = request.DateOfBirth; if (request.Gender is not null) student.Gender = request.Gender;
            if (request.Address is not null) student.Address = Trim(request.Address); if (request.Major is not null) student.Major = Trim(request.Major);
            if (request.Cohort is not null) student.Cohort = Trim(request.Cohort); if (request.ClassName is not null) student.ClassName = Trim(request.ClassName);
        }
        else if (account.Role == "MENTOR" && account.Mentor is { } mentor)
        {
            ValidateName(request.FullName, name);
            if (name is not null) mentor.FullName = name; if (request.Phone is not null) mentor.Phone = Trim(request.Phone);
            if (request.JobTitle is not null) mentor.JobTitle = Trim(request.JobTitle); if (request.Company is not null) mentor.Company = Trim(request.Company);
            if (request.Expertise is not null) mentor.Expertise = Trim(request.Expertise); if (request.YearsOfExperience is not null) mentor.YearsOfExperience = request.YearsOfExperience;
            if (request.LinkedinUrl is not null) mentor.LinkedinUrl = Trim(request.LinkedinUrl);
        }
        else if (account.Role == "INSTRUCTOR" && account.Instructor is { } instructor)
        {
            ValidateName(request.FullName, name);
            if (name is not null) instructor.FullName = name; if (request.Phone is not null) instructor.Phone = Trim(request.Phone);
            if (request.Department is not null) instructor.Department = Trim(request.Department); if (request.Expertise is not null) instructor.Expertise = Trim(request.Expertise);
        }
        else if (account.Role == "ADMIN") throw new ForbiddenException("Admins do not have an editable role profile");
        else throw new NotFoundException($"{account.Role[..1]}{account.Role[1..].ToLowerInvariant()} profile not found");
        await repository.SaveAsync(ct); return MapProfile(account);
    }

    public async Task ChangePasswordAsync(string email, ChangeOwnPasswordRequest request, CancellationToken ct)
    {
        var account = await ActiveAccount(email, ct);
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash)) throw new BadRequestException("Current password is incorrect");
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword); account.MustChangePassword = false; account.UpdatedAt = DateTime.UtcNow;
        await refreshTokens.DeleteForAccountAsync(account.Id, ct); await repository.SaveAsync(ct);
    }

    private async Task<Account> ActiveAccount(string email, CancellationToken ct)
    {
        var account = await repository.FindProfileAsync(email, ct) ?? throw new UnauthorizedException("Account not found");
        if (account.Status != "ACTIVE") throw new ForbiddenException("Account is not active");
        return account;
    }

    private static void ValidateName(string? raw, string? trimmed)
    {
        if (raw is not null && string.IsNullOrWhiteSpace(trimmed)) throw new BadRequestException("Full name cannot be blank");
    }
    private static string? Trim(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static NotificationResponse MapNotification(Notification n)
    {
        var key = n.ActionKey ?? LegacyAction(n.Type, n.ActionUrl);
        var raw = n.ActionParams ?? n.Payload; Dictionary<string, string> values = [];
        if (!string.IsNullOrWhiteSpace(raw)) try { values = JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? []; } catch (JsonException) { }
        return new(n.Id, n.Type, n.Title, n.Body, n.ActionUrl, n.EntityType, n.EntityId, n.Payload,
            new(key, values), n.ReadAt is not null, n.ReadAt, n.CreatedAt);
    }

    private static string LegacyAction(string type, string? url) => type switch
    {
        "GROUP_INVITATION_CREATED" => "OPEN_GROUP_INVITATIONS",
        "TASK_ASSIGNED" or "TASK_COMMENT_CREATED" or "TASK_STATUS_CHANGED" => "OPEN_TASK",
        "TIMELINE_ITEM_CREATED" or "TIMELINE_ITEM_UPDATED" => "OPEN_MILESTONE",
        "MILESTONE_SUBMISSION_CREATED" => "OPEN_SUBMISSION",
        "MILESTONE_SUBMISSION_GRADED" or "MILESTONE_GROUP_GRADED" => "OPEN_GRADES",
        "MENTOR_MEETING_BOOKED" or "MENTOR_MEETING_CANCELED" or "MENTOR_MEETING_CONFIRMED" or "MENTOR_MEETING_COMPLETED" => "OPEN_MEETING",
        "TERM_FEEDBACK_AVAILABLE" => "OPEN_FEEDBACK", _ => url is null ? "NONE" : "OPEN_GROUP"
    };

    private static SelfProfileResponse MapProfile(Account a)
    {
        StudentProfileResponse? student = a.Student is null ? null : new(a.Student.Id, a.Student.StudentCode, a.Student.FullName,
            a.Email, a.Student.Phone, a.Student.DateOfBirth, a.Student.Gender, a.Student.Address, a.Student.Major, a.Student.Cohort, a.Student.ClassName, a.Student.Status);
        MentorProfileResponse? mentor = a.Mentor is null ? null : new(a.Mentor.Id, a.Mentor.MentorCode, a.Mentor.FullName, a.Email,
            a.Mentor.Phone, a.Mentor.JobTitle, a.Mentor.Company, a.Mentor.Expertise, a.Mentor.YearsOfExperience, a.Mentor.LinkedinUrl, a.Mentor.Status);
        InstructorProfileResponse? instructor = a.Instructor is null ? null : new(a.Instructor.Id, a.Instructor.InstructorCode, a.Instructor.FullName,
            a.Email, a.Instructor.Phone, a.Instructor.Department, a.Instructor.Expertise, a.Instructor.Status);
        IReadOnlyList<StudentGroupMembershipResponse>? memberships = a.Student?.StudentGroupMembers.Select(x => new StudentGroupMembershipResponse(
            x.GroupId, x.Group.Term, x.Group.CourseCode, x.Group.GroupNo, x.Group.Name, x.Group.ProjectName, x.MemberRole, x.JoinedAt)).ToList();
        return new(a.Id, a.Email, a.Role, a.Status, a.MustChangePassword, a.CreatedAt, a.UpdatedAt, a.LastLoginAt,
            student, mentor, instructor, memberships);
    }
}
