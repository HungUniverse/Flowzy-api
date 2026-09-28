using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IMilestoneGradeRepository
{
    Task<MilestoneSubmission?> FindSubmissionAsync(long submissionId, CancellationToken cancellationToken = default);
    Task<MilestoneGrade?> FindBySubmissionIdAsync(long submissionId, CancellationToken cancellationToken = default);
    Task<StudentGroup?> FindGroupAsync(long groupId, CancellationToken cancellationToken = default);
    Task<List<MilestoneGrade>> FindByGroupIdAsync(long groupId, CancellationToken cancellationToken = default);
}
