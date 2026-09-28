using Flowzy.Service.Contracts;
using Flowzy.Service.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/imports")]
public sealed class ProblemImportController(IImportService service) : ControllerBase
{
    [HttpPost("problem-bank"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    { await using var input = file.OpenReadStream(); return Accepted(new ApiResponse<ImportResultResponse>(202, "Problem bank import job queued successfully", await service.QueueAsync(input, file.FileName, file.Length, "PROBLEM_BANK", User.Identity!.Name!, ct))); }
}
