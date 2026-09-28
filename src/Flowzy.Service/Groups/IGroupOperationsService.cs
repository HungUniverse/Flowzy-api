using Flowzy.Service.Contracts;

namespace Flowzy.Service.Groups;

public interface IGroupOperationsService
{
 Task<IReadOnlyList<GroupSummaryResponse>> ListAsync(string? search,string? status,string? neededRole,string? category,CancellationToken ct);
 Task<PageResponse<GroupSummaryResponse>> DiscoverAsync(int page,int size,string? name,decimal? gpa,string? role,string email,CancellationToken ct);
 Task<object> DetailAsync(long id,CancellationToken ct); Task<object> CreateAsync(CreateGroupRequest r,string email,CancellationToken ct);
 Task<object> UpdateAsync(long id,UpdateGroupRequest r,string email,CancellationToken ct); Task<object> CriteriaAsync(long id,UpdateGroupCriteriaRequest r,string email,CancellationToken ct);
 Task<IReadOnlyList<GroupSummaryResponse>> MineAsync(string kind,string email,string? term,string? course,CancellationToken ct);
 Task RemoveAsync(long groupId,long studentId,string email,CancellationToken ct); Task LeaveAsync(long groupId,string email,CancellationToken ct);
 Task TransferAsync(long groupId,long studentId,string email,CancellationToken ct); Task<object> AssignAsync(long groupId,string kind,long? accountId,CancellationToken ct);
 Task<object> LockAsync(long groupId,bool value,string email,CancellationToken ct);
}
