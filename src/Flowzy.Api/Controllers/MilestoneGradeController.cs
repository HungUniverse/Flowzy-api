using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Grades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize, Route("api/milestone-submissions")]
public sealed class MilestoneGradeController(IMilestoneGradeService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("{submissionId:long}/grades")]
    public async Task<ActionResult<ApiResponse<MilestoneGradeResponse>>> GetBySubmission(long submissionId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<MilestoneGradeResponse>.Success(
            await service.GetBySubmissionIdAsync(submissionId, Email, cancellationToken),
            "Milestone grade retrieved successfully"));

    [HttpGet("groups/{groupId:long}/grades")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MilestoneGradeResponse>>>> GetByGroup(long groupId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<MilestoneGradeResponse>>.Success(
            await service.GetByGroupIdAsync(groupId, Email, cancellationToken),
            "Grades retrieved successfully"));

    [HttpPost("bulk-grade")]
    public ActionResult<ApiResponse<object>> BulkGrade() =>
        NotFound(ApiResponse<object>.Error(404, "Bulk grading not supported"));
}
