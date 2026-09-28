using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Submissions;

public sealed class InstructorSubmissionService(IAccountRepository accounts,
    IInstructorSubmissionRepository submissions) : IInstructorSubmissionService
{
    public async Task<IReadOnlyList<MilestoneSubmissionResponse>> GetAsync(string? term, string? courseCode,
        long? milestoneId, long? groupId, SubmissionStatus? status, bool? late, string currentUserEmail,
        CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(currentUserEmail, cancellationToken)
            ?? throw new UnauthorizedException("User not found");
        if (account.Role != "INSTRUCTOR" || account.Status != "ACTIVE")
            throw new ForbiddenException("Only instructors can list their submissions");
        var instructor = account.Instructor ?? throw new ForbiddenException("Instructor profile not found");

        var result = await submissions.FindAllForInstructorAsync(instructor.Id, term, courseCode,
            milestoneId, groupId, status?.ToString(), late, cancellationToken);
        return result.Select(Map).ToList();
    }

    private static MilestoneSubmissionResponse Map(MilestoneSubmission submission)
    {
        var grade = submission.MilestoneGrade;
        return new MilestoneSubmissionResponse(submission.Id, submission.MilestoneId, submission.GroupId,
            submission.SubmittedBy, submission.FileUrl, submission.Comments, submission.SubmittedAt,
            submission.Late, submission.Status, submission.Version, grade?.Score,
            grade is null ? null : grade.MaxScore, grade?.Feedback, grade?.GradedAt,
            submission.CreatedAt, submission.UpdatedAt);
    }
}
