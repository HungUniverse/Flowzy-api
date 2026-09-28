using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "STUDENT"), Route("api/students")]
public sealed class StudentController(IStudentService service) : ControllerBase
{
    private string Email => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
        ?? throw new UnauthorizedException("Not authenticated");

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<StudentProfileResponse>>> GetById(long id,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<StudentProfileResponse>.Success(
            await service.GetByIdAsync(id, Email, cancellationToken),
            "Student profile retrieved successfully"));

    [HttpGet("ungrouped")]
    public async Task<ActionResult<ApiResponse<PageResponse<StudentProfileResponse>>>> GetUngrouped(
        [FromQuery] string? term, [FromQuery] string? courseCode, [FromQuery] string? search,
        [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<PageResponse<StudentProfileResponse>>.Success(
            await service.GetUngroupedAsync(term, courseCode, search, page, size, Email, cancellationToken),
            "Ungrouped students retrieved successfully"));
}
