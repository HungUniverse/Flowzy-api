using Flowzy.Service.Contracts;
using Flowzy.Service.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/admin/groups")]
public sealed class AdminGroupController(IAdminGroupService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<GroupSummaryResponse>>>> List(
        [FromQuery] int page = 0, [FromQuery] int size = 20, [FromQuery] string? search = null,
        [FromQuery] string? status = "ACTIVE", CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<PageResponse<GroupSummaryResponse>>.Success(
            await service.SearchAsync(page, size, search, status, cancellationToken),
            "Groups retrieved successfully"));
}
