using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Platform;

namespace Flowzy.Service.Groups;

public sealed class GroupProblemService(IGroupProblemRepository repository, IPlatformService platform,
    IGroupOperationsService groups, TimeProvider clock) : IGroupProblemService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<object> SelectProblemAsync(long groupId, long problemId, string email, CancellationToken ct)
    {
        await using var tx = await repository.BeginAsync(ct);
        var group = await WritableGroup(groupId, email, ct);
        var problem = await repository.ProblemAsync(problemId, ct) ?? throw new BadRequestException($"Problem not found with id: {problemId}");
        if (problem.Status != "ACTIVE") throw new BadRequestException("Selected problem must be ACTIVE");
        if (problem.SourceType != "OFFICIAL") throw new BadRequestException("Selected problem must be an official problem");
        group.SelectedProblemId = problem.Id; group.UpdatedAt = Now;
        await repository.SaveAsync(ct);
        var result = await groups.DetailAsync(groupId, ct);
        await tx.CommitAsync(ct); return result;
    }

    public async Task ClearProblemAsync(long groupId, string email, CancellationToken ct)
    {
        await using var tx = await repository.BeginAsync(ct);
        var group = await WritableGroup(groupId, email, ct);
        group.SelectedProblemId = null; group.UpdatedAt = Now;
        await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<ProblemDetailResponse> ProposeAsync(long groupId, ProposeGroupProblemRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.BeginAsync(ct);
        var group = await repository.GroupAsync(groupId, true, ct) ?? throw new NotFoundException($"Group not found with id: {groupId}");
        var leader = await VerifyLeader(group, email, ct); StudentTermWriteGuard.RequireWritable(group);
        var domain = await ActiveDomain(request.DomainCode, ct);
        var problem = new Problem { Code = $"SP-{groupId}-{Guid.NewGuid().ToString("N")[..8]}",
            SourceType = "SELF_PROPOSED", Status = "PENDING_REVIEW", ProposedByGroupId = groupId,
            ProposedByStudentId = leader.Id, CreatedAt = Now, UpdatedAt = Now };
        Apply(problem, request, domain.Id);
        repository.Add(problem); await repository.SaveAsync(ct);
        group.SelectedProblemId = problem.Id; group.UpdatedAt = Now;
        await repository.SaveAsync(ct);
        var result = await platform.GetProblemAsync(problem.Id, ct);
        await tx.CommitAsync(ct); return result;
    }

    public async Task<ProblemDetailResponse> UpdateProposalAsync(long groupId, long problemId, ProposeGroupProblemRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.BeginAsync(ct);
        await WritableGroup(groupId, email, ct);
        var problem = await PendingProposal(groupId, problemId, false, ct);
        var domain = await ActiveDomain(request.DomainCode, ct);
        Apply(problem, request, domain.Id); problem.UpdatedAt = Now;
        await repository.SaveAsync(ct);
        var result = await platform.GetProblemAsync(problem.Id, ct);
        await tx.CommitAsync(ct); return result;
    }

    public async Task DeleteProposalAsync(long groupId, long problemId, string email, CancellationToken ct)
    {
        await using var tx = await repository.BeginAsync(ct);
        var group = await WritableGroup(groupId, email, ct);
        var problem = await PendingProposal(groupId, problemId, true, ct);
        if (group.SelectedProblemId == problemId)
        {
            group.SelectedProblemId = null; group.UpdatedAt = Now;
            await repository.SaveAsync(ct);
        }
        repository.Remove(problem); await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<ProblemSummaryResponse>> ProposalsAsync(long groupId, string email, CancellationToken ct)
    {
        _ = await repository.GroupAsync(groupId, false, ct) ?? throw new ForbiddenException($"Group not found with id: {groupId}");
        await VerifyMember(groupId, email, ct);
        return (await repository.ProposalsAsync(groupId, ct)).Select(p => new ProblemSummaryResponse(p.Id, p.Code, p.Title,
            p.Domain?.Code, p.Domain?.Name, p.DifficultyLevel, p.SourceType, p.Status, p.StrategicTheme, p.ResearchArea,
            p.ProposedByGroupId, p.ProposedByGroup?.GroupNo, p.ProposedByGroup?.Name)).ToList();
    }

    private async Task<StudentGroup> WritableGroup(long id, string email, CancellationToken ct)
    {
        var group = await repository.GroupAsync(id, true, ct) ?? throw new NotFoundException($"Group not found with id: {id}");
        await VerifyLeader(group, email, ct); StudentTermWriteGuard.RequireWritable(group); return group;
    }
    private async Task<Student> VerifyMember(long id, string email, CancellationToken ct)
    {
        var student = await repository.StudentAsync(email, ct) ?? throw new ForbiddenException($"Student profile not found for email: {email}");
        if (!await repository.IsMemberAsync(id, student.Id, ct)) throw new ForbiddenException("Access denied: you do not belong to this group");
        return student;
    }
    private async Task<Student> VerifyLeader(StudentGroup group, string email, CancellationToken ct)
    {
        var student = await VerifyMember(group.Id, email, ct);
        if (group.LeaderStudentId != student.Id) throw new ForbiddenException("Access denied: only the group leader can perform this action");
        return student;
    }
    private async Task<ProblemDomain> ActiveDomain(string code, CancellationToken ct)
    {
        var domain = await repository.DomainAsync(code, ct) ?? throw new BadRequestException($"Problem domain not found with code: {code}");
        if (domain.Status != "ACTIVE") throw new BadRequestException("Problem domain must be ACTIVE");
        return domain;
    }
    private async Task<Problem> PendingProposal(long groupId, long id, bool deleting, CancellationToken ct)
    {
        var problem = await repository.ProblemAsync(id, ct) ?? throw new NotFoundException($"Problem not found with id: {id}");
        if (problem.ProposedByGroupId != groupId) throw new NotFoundException("Proposal not found in this group");
        var verb = deleting ? "deleted" : "edited";
        if (problem.SourceType != "SELF_PROPOSED") throw new BadRequestException($"Only self-proposed problems can be {verb} by the group");
        if (problem.Status != "PENDING_REVIEW") throw new ConflictException($"Only pending proposals can be {verb} before review");
        return problem;
    }
    private static void Apply(Problem problem, ProposeGroupProblemRequest request, long domain)
    {
        problem.DomainId = domain; problem.Title = request.Title; problem.Statement = request.Statement;
        problem.StrategicTheme = request.StrategicTheme; problem.ResearchArea = request.ResearchArea;
        problem.DifficultyLevel = request.DifficultyLevel; problem.ExpectedOutput = request.ExpectedOutput;
    }
}
