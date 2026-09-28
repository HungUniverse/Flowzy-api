using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize, Route("api/feedback")]
public sealed class FeedbackController(IFeedbackService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("me"), Authorize(Roles = "STUDENT")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TermFeedbackResponse>>>> Mine(
        [FromQuery] string? term, [FromQuery] FeedbackStatus? status, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<TermFeedbackResponse>>.Success(
            await service.GetOwnAsync(term, status, Email, cancellationToken),
            "Feedbacks retrieved successfully"));

    [HttpPut("{id:long}"), Authorize(Roles = "STUDENT")]
    public async Task<ActionResult<ApiResponse<TermFeedbackResponse>>> Submit(long id,
        [FromBody] SubmitFeedbackRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<TermFeedbackResponse>.Success(
            await service.SubmitAsync(id, request, Email, cancellationToken),
            "Feedback submitted successfully"));

    [HttpGet("received"), Authorize(Roles = "MENTOR,INSTRUCTOR")]
    public async Task<ActionResult<ApiResponse<FeedbackReceivedSummary>>> Received(
        [FromQuery] string? term, [FromQuery] string? courseCode, CancellationToken cancellationToken) =>
        Ok(ApiResponse<FeedbackReceivedSummary>.Success(
            await service.GetReceivedAsync(Email, term, courseCode, cancellationToken),
            "Feedbacks retrieved successfully"));
}
