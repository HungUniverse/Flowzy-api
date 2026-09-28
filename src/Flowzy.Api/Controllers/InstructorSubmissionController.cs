using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "INSTRUCTOR"), Route("api/instructor/submissions")]
public sealed class InstructorSubmissionController(IInstructorSubmissionService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MilestoneSubmissionResponse>>>> Get(
        [FromQuery] string? term, [FromQuery] string? courseCode, [FromQuery] long? milestoneId,
        [FromQuery] long? groupId, [FromQuery] SubmissionStatus? status, [FromQuery] bool? late,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<MilestoneSubmissionResponse>>.Success(
            await service.GetAsync(term, courseCode, milestoneId, groupId, status, late, Email,
                cancellationToken),
            "Instructor submissions retrieved successfully"));
}
