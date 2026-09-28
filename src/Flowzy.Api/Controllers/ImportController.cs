using Flowzy.Service.Contracts;
using Flowzy.Service.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/imports")]
public sealed class ImportController(IImportService service) : ControllerBase
{
    [HttpPost("students"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Students(IFormFile file, CancellationToken ct)
    { await using var input = file.OpenReadStream(); return Accepted(new ApiResponse<ImportResultResponse>(202, "Student import job queued successfully", await service.QueueAsync(input, file.FileName, file.Length, "STUDENT", User.Identity!.Name!, ct))); }
    [HttpPost("mentors"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Mentors(IFormFile file, CancellationToken ct)
    { await using var input = file.OpenReadStream(); return Accepted(new ApiResponse<ImportResultResponse>(202, "Mentor import job queued successfully", await service.QueueAsync(input, file.FileName, file.Length, "MENTOR", User.Identity!.Name!, ct))); }
    [HttpGet("{batchId:long}")]
    public async Task<ActionResult<ApiResponse<ImportBatchResponse>>> Status(long batchId, CancellationToken ct) => Ok(ApiResponse<ImportBatchResponse>.Success(await service.GetBatchAsync(batchId, ct)));
    [HttpGet("{batchId:long}/errors")]
    public async Task<ActionResult<ApiResponse<PageResponse<ImportRowErrorResponse>>>> Errors(long batchId, string? search, int? rowNumber,
        string? fieldName, string? errorCode, int page = 0, int size = 20, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<ImportRowErrorResponse>>.Success(await service.GetErrorsAsync(batchId, page, size, search, rowNumber, fieldName, errorCode, ct)));
    [HttpGet("templates/students")] public IActionResult StudentTemplate() => Download("STUDENT", "SU26_EXE101_Group_List_template.xlsx");
    [HttpGet("templates/mentors")] public IActionResult MentorTemplate() => Download("MENTOR", "mentor_ID_matrix_template.xlsx");
    [HttpGet("templates/problem-bank")] public IActionResult ProblemTemplate() => Download("PROBLEM_BANK", "Guideline_EXE101_problem_bank_template.xlsx");
    private FileContentResult Download(string target, string filename)
    { Response.Headers.ContentDisposition = "attachment; filename=\"" + filename + "\""; return File(ImportTemplates.Create(target), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"); }
}
