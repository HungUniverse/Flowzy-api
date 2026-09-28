using System.Text.RegularExpressions;
using Flowzy.Service.Contracts;
using Flowzy.Service.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/admin/feedback")]
public sealed partial class AdminFeedbackController(IAdminFeedbackService service) : ControllerBase
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<AdminFeedbackResponse>>>> List(
        [FromQuery] int page = 0, [FromQuery] int size = 20, [FromQuery] string? term = null,
        [FromQuery] string? courseCode = null, [FromQuery] FeedbackTargetType? targetType = null,
        [FromQuery] long? targetId = null, [FromQuery] string? targetSearch = null,
        [FromQuery] FeedbackStatus? status = null, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<PageResponse<AdminFeedbackResponse>>.Success(
            await service.SearchAsync(page, size, term, courseCode, targetType, targetId, targetSearch, status,
                cancellationToken), "Feedbacks retrieved successfully"));

    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Export([FromQuery] string? term, CancellationToken cancellationToken)
    {
        var bytes = await service.ExportAsync(term, cancellationToken);
        var safe = term is null ? "term" : Unsafe().Replace(term.Trim().ToUpperInvariant(), "-");
        Response.Headers.ContentDisposition = $"attachment; filename=\"feedback-{safe}.xlsx\"";
        return File(bytes, Xlsx);
    }
    [GeneratedRegex("[^A-Z0-9_-]")] private static partial Regex Unsafe();
}
