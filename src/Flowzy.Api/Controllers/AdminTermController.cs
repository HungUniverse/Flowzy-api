using Flowzy.Service.Contracts;
using Flowzy.Service.Terms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController,Authorize(Roles="ADMIN"),Route("api/admin/terms")]
public sealed class AdminTermController(IAcademicTermService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PageResponse<AcademicTermResponse>>>> List([FromQuery]int page=0,[FromQuery]int size=10,CancellationToken ct=default)=>Ok(ApiResponse<PageResponse<AcademicTermResponse>>.Success(await service.ListTermsAsync(page,size,ct),"Academic terms retrieved successfully"));
    [HttpPost] public async Task<ActionResult<ApiResponse<AcademicTermResponse>>> Create(CreateAcademicTermRequest request,CancellationToken ct)=>Ok(ApiResponse<AcademicTermResponse>.Success(await service.CreateTermAsync(request,ct),"Academic term created successfully"));
    [HttpPatch("{term}/close")] public async Task<ActionResult<ApiResponse<AcademicTermResponse>>> Close(string term,CancellationToken ct)=>Ok(ApiResponse<AcademicTermResponse>.Success(await service.CloseTermAsync(term,User.Identity!.Name!,ct),"Academic term closed successfully"));
    [HttpPost("{term}/archive-students")] public async Task<ActionResult<ApiResponse<ArchiveTermStudentsResponse>>> Archive(string term,CancellationToken ct)=>Ok(ApiResponse<ArchiveTermStudentsResponse>.Success(await service.ArchiveStudentsAsync(term,ct),"Eligible students archived successfully"));
    [HttpDelete("{term}")] public async Task<ActionResult<ApiResponse<object>>> Delete(string term,CancellationToken ct){await service.DeleteTermAsync(term,ct);return Ok(ApiResponse<object>.Success(null,"Academic term deleted successfully"));}
}
