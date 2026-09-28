using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Mentors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "MENTOR"), Route("api/mentor/availability")]
public sealed class MentorAvailabilityController(IMentorAvailabilityService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MentorAvailabilitySlotResponse>>> Create(
        [FromBody] CreateAvailabilitySlotRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MentorAvailabilitySlotResponse>.Success(
            await service.CreateAsync(request, Email, cancellationToken),
            "Availability slot created successfully"));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MentorAvailabilitySlotResponse>>>> List(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<MentorAvailabilitySlotResponse>>.Success(
            await service.ListAsync(Email, cancellationToken), "Availability slots retrieved successfully"));

    [HttpGet("me")]
    public Task<ActionResult<ApiResponse<IReadOnlyList<MentorAvailabilitySlotResponse>>>> ListMine(
        CancellationToken cancellationToken) => List(cancellationToken);

    [HttpPatch("{id:long}")]
    public async Task<ActionResult<ApiResponse<MentorAvailabilitySlotResponse>>> Update(long id,
        [FromBody] UpdateAvailabilitySlotRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<MentorAvailabilitySlotResponse>.Success(
            await service.UpdateAsync(id, request, Email, cancellationToken),
            "Availability slot updated successfully"));

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(long id, CancellationToken cancellationToken)
    {
        await service.CancelAsync(id, Email, cancellationToken);
        return Ok(ApiResponse<object>.Success(null, "Availability slot canceled successfully"));
    }
}
