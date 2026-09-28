using Flowzy.Service.Contracts;

namespace Flowzy.Service.Submissions;

public interface IMilestoneSubmissionService
{
    Task<MilestoneSubmissionResponse> GetSubmissionAsync(long id, string email, CancellationToken ct);
    Task<IReadOnlyList<MilestoneSubmissionResponse>> GetGroupSubmissionsAsync(long groupId, string email, CancellationToken ct);
    Task<IReadOnlyList<MilestoneSubmissionResponse>> GetMilestoneSubmissionsAsync(long milestoneId, string email, CancellationToken ct);
}
