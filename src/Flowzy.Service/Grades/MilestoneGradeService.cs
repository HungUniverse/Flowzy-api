using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Grades;

public sealed class MilestoneGradeService(
    IMilestoneGradeRepository grades,
    IAccountRepository accounts) : IMilestoneGradeService
{
    public async Task<MilestoneGradeResponse> GetBySubmissionIdAsync(long submissionId, string currentUserEmail,
        CancellationToken cancellationToken = default)
    {
        var submission = await grades.FindSubmissionAsync(submissionId, cancellationToken)
            ?? throw new NotFoundException("Milestone submission not found");
        await EnsureCanRetrieveAsync(submission.Group, currentUserEmail, cancellationToken);
        var grade = await grades.FindBySubmissionIdAsync(submissionId, cancellationToken)
            ?? throw new NotFoundException("Milestone grade not found");
        return Map(grade);
    }

    public async Task<IReadOnlyList<MilestoneGradeResponse>> GetByGroupIdAsync(long groupId, string currentUserEmail,
        CancellationToken cancellationToken = default)
    {
        var group = await grades.FindGroupAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Student group not found");
        await EnsureCanRetrieveAsync(group, currentUserEmail, cancellationToken);
        return (await grades.FindByGroupIdAsync(groupId, cancellationToken)).Select(Map).ToList();
    }

    public async Task<AverageGradeResponse> CalculateAverageForGroupAsync(long groupId, string currentUserEmail,
        CancellationToken cancellationToken = default)
    {
        var group = await grades.FindGroupAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Student group not found");
        await EnsureCanRetrieveAsync(group, currentUserEmail, cancellationToken);

        decimal weightedScores = 0;
        decimal weights = 0;
        foreach (var grade in await grades.FindByGroupIdAsync(groupId, cancellationToken))
        {
            var milestone = grade.Submission.Milestone;
            if (string.Equals(milestone.Status, "INACTIVE", StringComparison.Ordinal) || milestone.Weight is null)
            {
                continue;
            }
            weightedScores += grade.Score * milestone.Weight.Value;
            weights += milestone.Weight.Value;
        }

        var average = weights == 0
            ? 0.00m
            : Math.Round(weightedScores / weights, 2, MidpointRounding.AwayFromZero);
        return new AverageGradeResponse(average, average);
    }

    private async Task EnsureCanRetrieveAsync(StudentGroup group, string email, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedException("User not found");

        var allowed = string.Equals(account.Role, "ADMIN", StringComparison.Ordinal)
            || string.Equals(account.Role, "STUDENT", StringComparison.Ordinal)
                && account.Student is not null
                && (group.LeaderStudentId == account.Student.Id
                    || group.StudentGroupMembers.Any(x => x.StudentId == account.Student.Id))
            || string.Equals(account.Role, "INSTRUCTOR", StringComparison.Ordinal)
                && account.Instructor is not null
                && group.InstructorId == account.Instructor.Id;

        if (!allowed)
        {
            throw new ForbiddenException("You do not have permission to access grades for this group");
        }
    }

    private static MilestoneGradeResponse Map(MilestoneGrade grade) => new(
        grade.Id,
        grade.SubmissionId,
        grade.Score,
        grade.MaxScore,
        grade.Feedback,
        grade.InstructorId,
        grade.GradedAt);
}
