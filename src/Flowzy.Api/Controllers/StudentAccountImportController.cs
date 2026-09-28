using Flowzy.Service.Contracts;
using Flowzy.Service.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/imports")]
public sealed class StudentAccountImportController(IImportService service) : ControllerBase
{
    [HttpPost("student-accounts"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    { await using var input = file.OpenReadStream(); return Accepted(new ApiResponse<ImportResultResponse>(202, "Student account import job queued successfully", await service.QueueAsync(input, file.FileName, file.Length, "STUDENT_ACCOUNT", User.Identity!.Name!, ct))); }
    [HttpGet("templates/student-accounts")]
    public IActionResult Template()
    { Response.Headers.ContentDisposition = "attachment; filename=\"student_accounts_template.xlsx\""; return File(ImportTemplates.Create("STUDENT_ACCOUNT"), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"); }
}
