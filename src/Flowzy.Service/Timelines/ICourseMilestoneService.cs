using Flowzy.Service.Contracts;
namespace Flowzy.Service.Timelines;
public interface ICourseMilestoneService
{
    Task<CourseMilestoneResponse> CreateMilestoneAsync(CourseMilestoneRequest request, string email, CancellationToken ct);
    Task<IReadOnlyList<CourseMilestoneResponse>> GetMilestonesAsync(string? term, string? course, string email, CancellationToken ct);
    Task<CourseMilestoneResponse> GetMilestoneAsync(long id, string email, CancellationToken ct);
    Task<CourseMilestoneResponse> UpdateMilestoneAsync(long id, UpdateCourseMilestoneRequest request, string email, CancellationToken ct);
    Task DeleteMilestoneAsync(long id, string email, CancellationToken ct);
    Task<IReadOnlyList<CourseMilestoneResponse>> GroupMilestonesAsync(long group, string email, CancellationToken ct);
}
