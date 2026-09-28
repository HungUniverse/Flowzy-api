using Flowzy.Service.Contracts;

namespace Flowzy.Service.Grades;

public interface IMilestoneGradeService
{
    Task<MilestoneGradeResponse> GetBySubmissionIdAsync(long submissionId, string currentUserEmail,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MilestoneGradeResponse>> GetByGroupIdAsync(long groupId, string currentUserEmail,
        CancellationToken cancellationToken = default);
    Task<AverageGradeResponse> CalculateAverageForGroupAsync(long groupId, string currentUserEmail,
        CancellationToken cancellationToken = default);
}
