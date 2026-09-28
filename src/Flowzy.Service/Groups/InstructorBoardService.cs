using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Groups;

public sealed class InstructorBoardService(IInstructorBoardRepository repository) : IInstructorBoardService
{
    public async Task<InstructorBoardResponse> Get(string email, int page, int size, string? term, string? course, string? search, string assignment, CancellationToken ct)
    {
        assignment = string.IsNullOrEmpty(assignment) ? "ALL" : assignment.Trim();
        if (assignment is not ("ALL" or "AVAILABLE" or "MINE" or "OTHER")) throw new BadRequestException("Invalid parameter format: assignment");
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        var instructor = await Instructor(email, ct);
        var t = Normalize(term); var c = Normalize(course);
        var (rows, total) = await repository.Search(instructor.Id, t, c, Normalize(search), assignment, page, size, ct);
        var counts = await repository.Counts(instructor.Id, t, c, ct);
        var courses = await repository.Courses(instructor.Id, t, ct);
        return new(new(counts.Total, counts.Available, counts.Mine, counts.Other),
            courses.Select(x => new InstructorCourseCount(x.Course, x.Counts.Total, x.Counts.Available, x.Counts.Mine, x.Counts.Other)).ToList(),
            PageResponse<InstructorBoardItem>.Create(rows.Select(x => Map(x, instructor.Id)).ToList(), page, size, total));
    }
    public async Task<InstructorBoardItem> Claim(long id, string email, CancellationToken ct)
    {
        var instructor = await Instructor(email, ct);
        await using var tx = await repository.Begin(ct);
        // Match the group -> term lock order used by term close to avoid a cross-domain deadlock.
        var group = await repository.LockGroup(id, ct) ?? throw new NotFoundException($"Group not found with id: {id}");
        var term = await repository.LockTerm(group.Term, ct) ?? throw new ConflictException("Group is not in an open academic term");
        if (term.Status != "OPEN") throw new ConflictException("Group academic term is closed");
        if (group.Status != "ACTIVE") throw new ConflictException("Only active groups can be claimed");
        var detail = await repository.Detail(id, ct);
        if (detail.StudentGroupMembers.Count == 0) throw new ConflictException("Groups without members cannot be claimed");
        if (group.InstructorId is not null && group.InstructorId != instructor.Id) throw new ConflictException("Group has already been claimed by another instructor");
        if (group.InstructorId is null)
        {
            group.InstructorId = instructor.Id;
            await repository.Save(ct);
            detail = await repository.Detail(id, ct);
        }
        await tx.CommitAsync(ct);
        return Map(detail, instructor.Id);
    }
    private async Task<Instructor> Instructor(string email, CancellationToken ct)
    {
        var instructor = await repository.Instructor(email, ct) ?? throw new NotFoundException($"Instructor profile not found for email: {email}");
        if (instructor.Status != "ACTIVE" || instructor.Account.Status != "ACTIVE") throw new ForbiddenException("Inactive instructors cannot claim groups");
        return instructor;
    }
    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? "";
    private static InstructorBoardItem Map(StudentGroup g, long instructor)
    {
        var members = g.StudentGroupMembers.OrderBy(x => x.MemberRole != "LEADER").ThenBy(x => x.JoinedAt).ThenBy(x => x.Id)
            .Select(x => new InstructorBoardMember(x.StudentId, x.Student.StudentCode, x.Student.FullName, x.Student.Account.Email, x.Student.ClassName, x.Student.Major, x.MemberRole)).ToList();
        return new(g.Id, g.Term, g.CourseCode, g.GroupNo, g.Name, g.ProjectName, g.IdeaDescription, g.ResearchDomain, g.IsLocked, members.Count,
            g.MentorId, g.Mentor?.MentorCode, g.Mentor?.FullName, g.InstructorId, g.Instructor?.InstructorCode, g.Instructor?.FullName,
            g.InstructorId is null ? "AVAILABLE" : g.InstructorId == instructor ? "MINE" : "OTHER", members);
    }
}
