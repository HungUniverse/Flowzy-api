using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Mentors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "MENTOR"), Route("api/mentor/meeting-reports")]
public sealed partial class MentorMeetingReportController(IMentorMeetingReportService service) : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("terms")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MentorReportTermResponse>>>> Terms(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<MentorReportTermResponse>>.Success(
            await service.ListTermsAsync(Email, cancellationToken),
            "Meeting report terms retrieved successfully"));

    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Export([FromQuery] string? term, CancellationToken cancellationToken)
    {
        var bytes = await service.ExportAsync(term, Email, cancellationToken);
        Response.Headers.ContentDisposition = $"attachment; filename=\"mentor-meeting-report-{SafeTerm(term)}.xlsx\"";
        return File(bytes, XlsxContentType);
    }

    private static string SafeTerm(string? term) => term is null ? "term"
        : UnsafeFileCharacters().Replace(term.Trim().ToUpperInvariant(), "-");

    [GeneratedRegex("[^A-Z0-9_-]")]
    private static partial Regex UnsafeFileCharacters();
}
