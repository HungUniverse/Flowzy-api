using Flowzy.Service.Contracts;

namespace Flowzy.Service.Submissions;

public interface IInstructorSubmissionService
{
    Task<IReadOnlyList<MilestoneSubmissionResponse>> GetAsync(string? term, string? courseCode,
        long? milestoneId, long? groupId, SubmissionStatus? status, bool? late, string currentUserEmail,
        CancellationToken cancellationToken = default);
}
