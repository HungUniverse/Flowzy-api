using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IMilestoneSubmissionRepository
{
    Task<MilestoneSubmission?> FindAsync(long id, CancellationToken ct);
    Task<StudentGroup?> GroupAsync(long id, CancellationToken ct);
    Task<CourseMilestone?> MilestoneAsync(long id, CancellationToken ct);
    Task<Student?> StudentAsync(string email, CancellationToken ct);
    Task<Instructor?> InstructorAsync(string email, CancellationToken ct);
    Task<bool> IsMemberAsync(long groupId, long studentId, CancellationToken ct);
    Task<List<MilestoneSubmission>> ByGroupAsync(long id, CancellationToken ct);
    Task<List<MilestoneSubmission>> ByMilestoneAsync(long id, long currentInstructorId, CancellationToken ct);
}
