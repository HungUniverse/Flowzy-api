using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Submissions;

public sealed class MilestoneSubmissionService(IMilestoneSubmissionRepository repository, IAccountRepository accounts) : IMilestoneSubmissionService
{
    public async Task<MilestoneSubmissionResponse> GetSubmissionAsync(long id, string email, CancellationToken ct)
    {
        var submission = await repository.FindAsync(id, ct) ?? throw new NotFoundException("Milestone submission not found");
        await CanViewGroup(submission.Group, email, "You do not have permission to view this submission", ct);
        return Map(submission);
    }

    public async Task<IReadOnlyList<MilestoneSubmissionResponse>> GetGroupSubmissionsAsync(long groupId, string email, CancellationToken ct)
    {
        var group = await repository.GroupAsync(groupId, ct) ?? throw new NotFoundException("Student group not found");
        await CanViewGroup(group, email, "You do not have permission to view submissions for this group", ct);
        return (await repository.ByGroupAsync(groupId, ct)).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<MilestoneSubmissionResponse>> GetMilestoneSubmissionsAsync(long milestoneId, string email, CancellationToken ct)
    {
        var milestone = await repository.MilestoneAsync(milestoneId, ct) ?? throw new NotFoundException("Course milestone not found");
        var account = await accounts.FindByEmailAsync(email, ct) ?? throw new UnauthorizedException("User not found");
        if (account.Role != "INSTRUCTOR") throw new ForbiddenException("Only the assigned instructor can view milestone submissions");
        var instructor = await repository.InstructorAsync(email, ct) ?? throw new ForbiddenException("Instructor profile not found");
        if (milestone.InstructorId != instructor.Id) throw new ForbiddenException("You do not manage this timeline milestone");
        return (await repository.ByMilestoneAsync(milestoneId, instructor.Id, ct)).Select(Map).ToList();
    }

    private async Task CanViewGroup(StudentGroup group, string email, string error, CancellationToken ct)
    {
        var account = await accounts.FindByEmailAsync(email, ct) ?? throw new UnauthorizedException("User not found");
        if (account.Role == "ADMIN") return;
        if (account.Role == "STUDENT")
        {
            var student = await repository.StudentAsync(email, ct);
            if (student is not null && (group.LeaderStudentId == student.Id || await repository.IsMemberAsync(group.Id, student.Id, ct))) return;
        }
        if (account.Role == "INSTRUCTOR" && group.InstructorId is not null)
        {
            var instructor = await repository.InstructorAsync(email, ct);
            if (instructor is not null && group.InstructorId == instructor.Id) return;
        }
        throw new ForbiddenException(error);
    }

    private static MilestoneSubmissionResponse Map(MilestoneSubmission e) => new(e.Id, e.MilestoneId, e.GroupId,
        e.SubmittedBy, e.FileUrl, e.Comments, e.SubmittedAt, e.Late, e.Status, e.Version,
        e.MilestoneGrade?.Score, e.MilestoneGrade?.MaxScore, e.MilestoneGrade?.Feedback, e.MilestoneGrade?.GradedAt,
        e.CreatedAt, e.UpdatedAt);
}
