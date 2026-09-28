using Flowzy.Service.Contracts;

namespace Flowzy.Service.Admin;

public interface IAdminUserService
{
    Task<PageResponse<AdminUserSummaryResponse>> SearchAsync(int page, int size, string? search, string? role, string? status, CancellationToken cancellationToken);
    Task<AdminUserDetailResponse> GetAsync(long id, CancellationToken cancellationToken);
    Task<AdminUserDetailResponse> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken);
    Task<AdminUserDetailResponse> UpdateAsync(long id, UpdateAdminUserRequest request, string currentEmail, CancellationToken cancellationToken);
    Task DeleteAsync(long id, string currentEmail, CancellationToken cancellationToken);
    Task ResetPasswordAsync(long id, string password, CancellationToken cancellationToken);
    Task ChangePasswordAsync(string email, string password, CancellationToken cancellationToken);
}
