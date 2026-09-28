using Flowzy.Service.Contracts;

namespace Flowzy.Service.Problems;

public interface IInstructorProblemService
{
    Task<IReadOnlyList<ProblemSummaryResponse>> GetPendingAsync(string instructorEmail,
        CancellationToken cancellationToken = default);
    Task<ProblemDetailResponse> ReviewAsync(long problemId, ReviewProblemRequest request, string instructorEmail,
        CancellationToken cancellationToken = default);
}
