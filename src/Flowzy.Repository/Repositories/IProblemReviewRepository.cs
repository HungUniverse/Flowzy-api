using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IProblemReviewRepository
{
    Task<List<Problem>> FindPendingForInstructorAsync(long instructorId, CancellationToken cancellationToken = default);
    Task<Problem?> FindForReviewAsync(long problemId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
