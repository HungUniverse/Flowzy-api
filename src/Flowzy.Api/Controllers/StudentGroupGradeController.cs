using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Grades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize, Route("api/student-groups")]
public sealed class StudentGroupGradeController(IMilestoneGradeService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("{groupId:long}/average-grade")]
    public async Task<ActionResult<ApiResponse<AverageGradeResponse>>> Average(long groupId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<AverageGradeResponse>.Success(
            await service.CalculateAverageForGroupAsync(groupId, Email, cancellationToken),
            "Average grade calculated successfully"));
}
