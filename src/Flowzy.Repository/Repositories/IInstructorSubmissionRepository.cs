using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IInstructorSubmissionRepository
{
    Task<List<MilestoneSubmission>> FindAllForInstructorAsync(long instructorId, string? term,
        string? courseCode, long? milestoneId, long? groupId, string? status, bool? late,
        CancellationToken cancellationToken = default);
}
