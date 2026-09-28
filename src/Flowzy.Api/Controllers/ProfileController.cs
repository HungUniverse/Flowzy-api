using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize, Route("api/profile")]
public sealed class ProfileController(IPlatformService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<SelfProfileResponse>>> Me(CancellationToken ct) =>
        Ok(ApiResponse<SelfProfileResponse>.Success(await service.GetProfileAsync(Email, ct), "Profile retrieved successfully"));

    [HttpPatch("me")]
    public async Task<ActionResult<ApiResponse<SelfProfileResponse>>> Update(UpdateSelfProfileRequest request, CancellationToken ct) =>
        Ok(ApiResponse<SelfProfileResponse>.Success(await service.UpdateProfileAsync(Email, request, ct), "Profile updated successfully"));

    [HttpPatch("me/password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangeOwnPasswordRequest request, CancellationToken ct)
    {
        await service.ChangePasswordAsync(Email, request, ct);
        return Ok(ApiResponse<object>.Success(null, "Password changed successfully"));
    }
}
