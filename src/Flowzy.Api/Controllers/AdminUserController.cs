using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Admin;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/admin/users")]
public sealed class AdminUserController(IAdminUserService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<AdminUserSummaryResponse>>>> List([FromQuery] int page = 0, [FromQuery] int size = 10,
        [FromQuery] string? search = null, [FromQuery] string? role = null, [FromQuery] string? status = null, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<AdminUserSummaryResponse>>.Success(await service.SearchAsync(page, size, search, role, status, ct), "Users retrieved successfully"));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<AdminUserDetailResponse>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResponse<AdminUserDetailResponse>.Success(await service.GetAsync(id, ct), "User details retrieved successfully"));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminUserDetailResponse>>> Create(CreateAdminUserRequest request, CancellationToken ct) =>
        Ok(ApiResponse<AdminUserDetailResponse>.Success(await service.CreateAsync(request, ct), "User created successfully"));

    [HttpPatch("{id:long}")]
    public async Task<ActionResult<ApiResponse<AdminUserDetailResponse>>> Update(long id, UpdateAdminUserRequest request, CancellationToken ct) =>
        Ok(ApiResponse<AdminUserDetailResponse>.Success(await service.UpdateAsync(id, request, Email, ct), "User updated successfully"));

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    { await service.DeleteAsync(id, Email, ct); return Ok(ApiResponse<object>.Success(null, "User deleted successfully")); }

    [HttpPost("{id:long}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> Reset(long id, ResetUserPasswordRequest request, CancellationToken ct)
    { await service.ResetPasswordAsync(id, request.NewPassword, ct); return Ok(ApiResponse<object>.Success(null, "Password reset successfully")); }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> Change(AdminChangePasswordRequest request, CancellationToken ct)
    { await service.ChangePasswordAsync(request.Email, request.NewPassword, ct); return Ok(ApiResponse<object>.Success(null, "Password changed successfully")); }
}
