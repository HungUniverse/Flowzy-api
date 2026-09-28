using Flowzy.Service.Contracts;

namespace Flowzy.Service.Groups;

public interface IAdminGroupService
{
    Task<PageResponse<GroupSummaryResponse>> SearchAsync(int page, int size, string? search, string? status,
        CancellationToken cancellationToken = default);
}
