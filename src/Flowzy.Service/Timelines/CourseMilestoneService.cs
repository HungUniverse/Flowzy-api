using System.Globalization;
using System.Text.Json;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Timelines;

public sealed class CourseMilestoneService(ICourseMilestoneRepository repository, TimeProvider clock) : ICourseMilestoneService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public async Task<CourseMilestoneResponse> CreateMilestoneAsync(CourseMilestoneRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var instructor = await Instructor(email, true, ct);
        var term = request.Term.Trim(); var course = request.CourseCode.Trim(); var title = request.Title.Trim();
        var academic = await repository.Term(term, ct) ?? throw new BadRequestException("Academic term does not exist");
        if (academic.Status != "OPEN") throw new BadRequestException("Academic term is closed");
        if ((await repository.AssignedGroups(instructor.Id, term, course, ct)).Count == 0)
            throw new ForbiddenException("You must have at least one assigned group in this term and course");
        if (request.DeadlineAt is null) throw new BadRequestException("Deadline is required");
        var scope = await repository.Scope(instructor.Id, term, course, ct);
        Unique(scope, title, null);
        var max = request.MaxScore ?? 10;
        if (max <= 0) throw new BadRequestException("Max score must be greater than 0");
        Weight(scope, null, request.Weight, "ACTIVE");
        var milestone = new CourseMilestone { InstructorId = instructor.Id, Instructor = instructor, Term = term, CourseCode = course,
            Title = title, Description = request.Description, Weight = request.Weight, DueDate = request.DeadlineAt, MaxScore = max,
            Position = request.Position ?? (scope.Count == 0 ? 0 : scope.Max(x => x.Position) + 1), Type = "TIMELINE", Status = "ACTIVE", CreatedAt = Now, UpdatedAt = Now };
        repository.Add(milestone); await repository.Save(ct); await repository.Reload(milestone, ct);
        await Notify(milestone, true, ct); await repository.Save(ct); await tx.CommitAsync(ct);
        return Map(milestone);
    }
    public async Task<CourseMilestoneResponse> UpdateMilestoneAsync(long id, UpdateCourseMilestoneRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var instructor = await Instructor(email, true, ct); var milestone = await Owned(id, instructor.Id, ct);
        var title = request.Title.Trim(); var scope = await repository.Scope(instructor.Id, milestone.Term, milestone.CourseCode, ct);
        Unique(scope, title, id);
        if (request.DeadlineAt is null) throw new BadRequestException("Deadline is required");
        var max = request.MaxScore ?? milestone.MaxScore;
        if (max <= 0) throw new BadRequestException("Max score must be greater than 0");
        if (await repository.HasHigherGrade(id, max, ct)) throw new BadRequestException("Max score cannot be lower than an existing grade");
        Weight(scope, id, request.Weight, request.Status ?? milestone.Status);
        var oldDeadline = milestone.DueDate;
        milestone.Title = title; milestone.Description = request.Description; milestone.Weight = request.Weight;
        milestone.DueDate = request.DeadlineAt; milestone.MaxScore = max;
        if (request.Position is not null) milestone.Position = request.Position.Value;
        if (request.Status is not null) milestone.Status = request.Status;
        await repository.Save(ct); await repository.Reload(milestone, ct);
        if (milestone.Status == "ACTIVE") await Notify(milestone, false, ct);
        if (oldDeadline != request.DeadlineAt)
            foreach (var submission in await repository.Submissions(id, ct)) submission.Late = submission.SubmittedAt > request.DeadlineAt;
        await repository.Save(ct); await tx.CommitAsync(ct); return Map(milestone);
    }
    public async Task DeleteMilestoneAsync(long id, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var instructor = await Instructor(email, true, ct); var milestone = await Owned(id, instructor.Id, ct);
        milestone.Status = "ARCHIVED"; await repository.Save(ct); await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<CourseMilestoneResponse>> GetMilestonesAsync(string? term, string? course, string email, CancellationToken ct)
    {
        var account = await Account(email, ct); term = Optional(term); course = Optional(course);
        if (account.Role == "INSTRUCTOR")
        {
            var instructor = await Instructor(email, false, ct);
            return (await repository.Owned(instructor.Id, ct)).Where(x => (term is null || Same(x.Term, term)) && (course is null || Same(x.CourseCode, course)))
                .OrderBy(x => x.Term, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.CourseCode, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Position).ThenBy(x => x.Id).Select(Map).ToList();
        }
        if (account.Role != "STUDENT") throw new ForbiddenException("You cannot view this instructor timeline");
        var student = await Student(email, ct); var groups = await repository.StudentGroups(student.Id, ct);
        if (term is null && course is null)
        {
            var result = new List<CourseMilestoneResponse>();
            foreach (var group in groups) result.AddRange(await Visible(group, ct));
            return result.DistinctBy(x => x.Id).OrderBy(x => x.Term, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.CourseCode, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Position).ToList();
        }
        if (term is null || course is null) throw new BadRequestException("Both term and courseCode are required when filtering student timelines");
        // Membership lookup in Java is case-sensitive, unlike the milestone scope query.
        var memberGroup = groups.FirstOrDefault(x => x.Term == term && x.CourseCode == course)
            ?? throw new ForbiddenException("You are not in a group for this term and course");
        return await Visible(memberGroup, ct);
    }
    public async Task<CourseMilestoneResponse> GetMilestoneAsync(long id, string email, CancellationToken ct)
    {
        var milestone = await Get(id, ct); var account = await Account(email, ct);
        if (account.Role == "INSTRUCTOR") { await Owned(id, (await Instructor(email, false, ct)).Id, ct); return Map(milestone); }
        if (account.Role == "STUDENT")
        {
            var student = await Student(email, ct);
            var group = (await repository.StudentGroups(student.Id, ct)).FirstOrDefault(x => x.Term == milestone.Term && x.CourseCode == milestone.CourseCode);
            if (group?.InstructorId is not null && group.InstructorId == milestone.InstructorId && milestone.Status != "ARCHIVED") return Map(milestone);
        }
        throw new ForbiddenException("Milestone is not visible to you");
    }
    public async Task<IReadOnlyList<CourseMilestoneResponse>> GroupMilestonesAsync(long id, string email, CancellationToken ct)
    {
        var group = await repository.Group(id, ct) ?? throw new NotFoundException("Student group not found");
        var account = await Account(email, ct);
        if (account.Role == "INSTRUCTOR" && group.InstructorId == (await Instructor(email, false, ct)).Id) return await Visible(group, ct);
        if (account.Role == "STUDENT")
        {
            var student = await Student(email, ct);
            if (group.StudentGroupMembers.Any(x => x.StudentId == student.Id)) return await Visible(group, ct);
        }
        throw new ForbiddenException("You cannot view this group's timeline");
    }
    private async Task<IReadOnlyList<CourseMilestoneResponse>> Visible(StudentGroup group, CancellationToken ct) => group.InstructorId is null ? []
        : (await repository.Scope(group.InstructorId.Value, group.Term, group.CourseCode, ct)).Where(x => x.Status is not ("ARCHIVED" or "INACTIVE")).Select(Map).ToList();
    private async Task<Account> Account(string email, CancellationToken ct)
    {
        var account = await repository.Account(email, ct) ?? throw new UnauthorizedException("User not found");
        if (account.Status != "ACTIVE") throw new ForbiddenException("Account is inactive");
        return account;
    }
    private async Task<Instructor> Instructor(string email, bool locked, CancellationToken ct)
    {
        if ((await Account(email, ct)).Role != "INSTRUCTOR") throw new ForbiddenException("Only instructors can manage timelines");
        var instructor = await repository.Instructor(email, locked, ct) ?? throw new ForbiddenException("Instructor profile not found");
        if (instructor.Status != "ACTIVE") throw new ForbiddenException("Instructor profile is inactive");
        return instructor;
    }
    private async Task<Student> Student(string email, CancellationToken ct) => await repository.Student(email, ct) ?? throw new ForbiddenException("Student profile not found");
    private async Task<CourseMilestone> Get(long id, CancellationToken ct) => await repository.Get(id, ct) ?? throw new NotFoundException("Timeline milestone not found");
    private async Task<CourseMilestone> Owned(long id, long instructor, CancellationToken ct)
    { var milestone = await Get(id, ct); if (milestone.InstructorId != instructor) throw new ForbiddenException("You do not manage this timeline milestone"); return milestone; }
    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Unique(IEnumerable<CourseMilestone> scope, string title, long? id)
    { if (scope.Any(x => x.Id != id && Same(x.Title, title))) throw new BadRequestException("Timeline milestone title must be unique in your term and course"); }
    private static void Weight(IEnumerable<CourseMilestone> scope, long? id, int? weight, string status)
    { if (status == "ACTIVE" && scope.Where(x => x.Id != id && x.Status == "ACTIVE").Sum(x => x.Weight ?? 0) + (weight ?? 0) > 100) throw new BadRequestException("Total active milestone weight cannot exceed 100"); }
    private async Task Notify(CourseMilestone milestone, bool created, CancellationToken ct)
    {
        var groups = await repository.AssignedGroups(milestone.InstructorId!.Value, milestone.Term, milestone.CourseCode, ct);
        var type = created ? "TIMELINE_ITEM_CREATED" : "TIMELINE_ITEM_UPDATED"; var id = milestone.Id.ToString(CultureInfo.InvariantCulture);
        var stamp = milestone.UpdatedAt.ToUniversalTime(); var fraction = stamp.Ticks % TimeSpan.TicksPerSecond;
        var key = stamp.ToString(fraction == 0 ? "yyyy-MM-dd'T'HH:mm:ss'Z'" : fraction % TimeSpan.TicksPerMillisecond == 0 ? "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" : "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);
        foreach (var recipient in groups.Where(x => x.LeaderStudent is not null).Select(x => x.LeaderStudent!.AccountId).Distinct())
            await repository.Notify(new Notification { RecipientId = recipient, Type = type, Title = created ? "New Milestone Released" : "Milestone Updated",
                Body = $"Milestone '{milestone.Title}' has been {(created ? "released" : "updated")} for {milestone.CourseCode} ({milestone.Term}).",
                ActionKey = "OPEN_MILESTONE", ActionParams = JsonSerializer.Serialize(new { milestoneId = id, term = milestone.Term, courseCode = milestone.CourseCode }),
                EntityType = "CourseMilestone", EntityId = id, EventKey = $"{type}:{id}:{key}", CreatedAt = Now, UpdatedAt = Now }, ct);
    }
    private static CourseMilestoneResponse Map(CourseMilestone m) => new(m.Id, m.Term, m.CourseCode, m.Title, m.Description, m.Weight, m.DueDate, m.MaxScore, m.Position, m.Status, m.InstructorId, m.Instructor?.FullName);
}
