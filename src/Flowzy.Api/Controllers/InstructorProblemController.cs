using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize, Route("api/instructor/problems")]
public sealed class InstructorProblemController(IInstructorProblemService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProblemSummaryResponse>>>> Pending(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<ProblemSummaryResponse>>.Success(
            await service.GetPendingAsync(Email, cancellationToken),
            "Pending problems retrieved successfully"));

    [HttpPatch("{problemId:long}/review")]
    public async Task<ActionResult<ApiResponse<ProblemDetailResponse>>> Review(long problemId,
        [FromBody] ReviewProblemRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProblemDetailResponse>.Success(
            await service.ReviewAsync(problemId, request, Email, cancellationToken),
            "Problem reviewed successfully"));
}
