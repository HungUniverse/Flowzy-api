using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize, Route("api/notifications")]
public sealed class NotificationController(IPlatformService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<NotificationResponse>>>> Get([FromQuery] int page = 0,
        [FromQuery] int size = 20, [FromQuery] bool unreadOnly = false, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<NotificationResponse>>.Success(await service.GetNotificationsAsync(Email, unreadOnly, page, size, ct),
            "Notifications retrieved successfully"));

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<long>>> Count(CancellationToken ct) =>
        Ok(ApiResponse<long>.Success(await service.GetUnreadCountAsync(Email, ct), "Unread count retrieved successfully"));

    [HttpPatch("{id:long}/read")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> Read(long id, CancellationToken ct) =>
        Ok(ApiResponse<NotificationResponse>.Success(await service.MarkNotificationReadAsync(id, Email, ct), "Notification marked as read successfully"));

    [HttpPatch("read-all")]
    public async Task<ActionResult<ApiResponse<long>>> ReadAll(CancellationToken ct) =>
        Ok(ApiResponse<long>.Success(await service.MarkAllNotificationsReadAsync(Email, ct), "All notifications marked as read successfully"));
}
