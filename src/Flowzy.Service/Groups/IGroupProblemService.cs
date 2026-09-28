using Flowzy.Service.Contracts;

namespace Flowzy.Service.Groups;

public interface IGroupProblemService
{
    Task<object> SelectProblemAsync(long groupId, long problemId, string email, CancellationToken ct);
    Task ClearProblemAsync(long groupId, string email, CancellationToken ct);
    Task<ProblemDetailResponse> ProposeAsync(long groupId, ProposeGroupProblemRequest request, string email, CancellationToken ct);
    Task<ProblemDetailResponse> UpdateProposalAsync(long groupId, long problemId, ProposeGroupProblemRequest request, string email, CancellationToken ct);
    Task DeleteProposalAsync(long groupId, long problemId, string email, CancellationToken ct);
    Task<IReadOnlyList<ProblemSummaryResponse>> ProposalsAsync(long groupId, string email, CancellationToken ct);
}
