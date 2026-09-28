using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Problems;

public sealed class InstructorProblemService(
    IProblemReviewRepository problems,
    IAccountRepository accounts) : IInstructorProblemService
{
    public async Task<IReadOnlyList<ProblemSummaryResponse>> GetPendingAsync(string instructorEmail,
        CancellationToken cancellationToken = default)
    {
        var instructor = await RequireActiveInstructorAsync(instructorEmail, cancellationToken);
        return (await problems.FindPendingForInstructorAsync(instructor.Id, cancellationToken)).Select(MapSummary).ToList();
    }

    public async Task<ProblemDetailResponse> ReviewAsync(long problemId, ReviewProblemRequest request,
        string instructorEmail, CancellationToken cancellationToken = default)
    {
        var instructor = await RequireActiveInstructorAsync(instructorEmail, cancellationToken);
        var reviewer = instructor.Account;
        var problem = await problems.FindForReviewAsync(problemId, cancellationToken)
            ?? throw new NotFoundException($"Problem not found with ID: {problemId}");

        if (problem.SourceType != "SELF_PROPOSED")
        {
            throw new BadRequestException("Only self-proposed problems can be reviewed");
        }
        if (problem.Status != "PENDING_REVIEW")
        {
            throw new BadRequestException("Problem is not in PENDING_REVIEW status");
        }
        if (problem.ProposedByGroup?.InstructorId != instructor.Id)
        {
            throw new ForbiddenException("You can only review problems proposed by your assigned groups");
        }

        var status = request.Status.Trim().ToUpperInvariant();
        if (status is not ("APPROVED" or "REJECTED"))
        {
            throw new BadRequestException("Review status must be APPROVED or REJECTED");
        }
        if (status == "REJECTED" && string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new BadRequestException("Comment is required when rejecting a proposal");
        }

        if (status == "APPROVED")
        {
            problem.SourceType = "OFFICIAL";
            problem.Status = "ACTIVE";
        }
        else
        {
            problem.Status = "REJECTED";
            if (problem.ProposedByGroup?.SelectedProblemId == problem.Id)
            {
                problem.ProposedByGroup.SelectedProblemId = null;
            }
        }
        problem.ReviewComment = request.Comment;
        problem.ReviewedByAccountId = reviewer.Id;
        problem.ReviewedByAccount = reviewer;
        problem.ReviewedAt = DateTime.UtcNow;
        problem.UpdatedAt = DateTime.UtcNow;
        await problems.SaveChangesAsync(cancellationToken);
        return MapDetail(problem);
    }

    private async Task<Instructor> RequireActiveInstructorAsync(string email, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedException("Instructor account not found");
        if (account.Role != "INSTRUCTOR" || account.Status != "ACTIVE")
        {
            throw new ForbiddenException("Only active instructors can review problems");
        }
        var instructor = account.Instructor
            ?? throw new ForbiddenException("Instructor profile not found");
        if (instructor.Status != "ACTIVE")
        {
            throw new ForbiddenException("Instructor profile is inactive");
        }
        return instructor;
    }

    private static ProblemSummaryResponse MapSummary(Problem x) => new(x.Id, x.Code, x.Title,
        x.Domain?.Code, x.Domain?.Name, x.DifficultyLevel, x.SourceType, x.Status, x.StrategicTheme, x.ResearchArea,
        x.ProposedByGroupId, x.ProposedByGroup?.GroupNo, x.ProposedByGroup?.Name);

    private static ProblemDetailResponse MapDetail(Problem x) => new(x.Id, x.Code, x.Title, x.Statement,
        x.StrategicTheme, x.ResearchArea, x.DifficultyLevel, x.ExpectedOutput, x.OwnerLab, x.SuggestedCourses,
        x.DriveFolderLink, x.SourceType, x.Status,
        x.Domain is null ? null : new DomainInfo(x.Domain.Id, x.Domain.Code, x.Domain.Name),
        x.ProposedByGroup is null ? null : new GroupInfo(x.ProposedByGroup.Id, x.ProposedByGroup.GroupNo, x.ProposedByGroup.Name),
        x.ProposedByStudent is null ? null : new StudentInfo(x.ProposedByStudent.Id, x.ProposedByStudent.StudentCode, x.ProposedByStudent.FullName),
        x.ReviewComment,
        x.ReviewedByAccount is null ? null : new ReviewerInfo(x.ReviewedByAccount.Id, x.ReviewedByAccount.Email),
        x.ReviewedAt, x.CreatedAt, x.UpdatedAt);
}
