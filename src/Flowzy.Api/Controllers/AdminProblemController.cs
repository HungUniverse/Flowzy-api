using Flowzy.Service.Contracts;
using Flowzy.Service.PortedDomains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize(Roles="ADMIN"), Route("api/admin")]
public sealed class AdminProblemController(IPortedDomainService service) : ControllerBase
{
    [HttpPost("problem-domains")]
    public async Task<ActionResult<ApiResponse<ProblemDomainResponse>>> CreateDomain(ProblemDomainRequest request,CancellationToken ct)=>Ok(ApiResponse<ProblemDomainResponse>.Success(await service.CreateProblemDomainAsync(request,ct),"Problem domain created successfully"));
    [HttpPatch("problem-domains/{id:long}")]
    public async Task<ActionResult<ApiResponse<ProblemDomainResponse>>> UpdateDomain(long id,UpdateProblemDomainRequest request,CancellationToken ct)=>Ok(ApiResponse<ProblemDomainResponse>.Success(await service.UpdateProblemDomainAsync(id,request,ct),"Problem domain updated successfully"));
    [HttpPost("problems")]
    public async Task<ActionResult<ApiResponse<ProblemDetailResponse>>> CreateProblem(ProblemWriteRequest request,CancellationToken ct)=>Ok(ApiResponse<ProblemDetailResponse>.Success(await service.CreateProblemAsync(request,ct),"Official problem created successfully"));
    [HttpPatch("problems/{id:long}")]
    public async Task<ActionResult<ApiResponse<ProblemDetailResponse>>> UpdateProblem(long id,ProblemPatchRequest request,CancellationToken ct)=>Ok(ApiResponse<ProblemDetailResponse>.Success(await service.UpdateProblemAsync(id,request,ct),"Problem updated successfully"));
    [HttpPatch("problems/{id:long}/status")]
    public async Task<ActionResult<ApiResponse<ProblemDetailResponse>>> Status(long id,UpdateProblemStatusRequest request,CancellationToken ct)=>Ok(ApiResponse<ProblemDetailResponse>.Success(await service.SetProblemStatusAsync(id,request.Status,ct),"Problem status updated successfully"));
    [HttpPatch("problems/{id:long}/review")]
    public async Task<ActionResult<ApiResponse<ProblemDetailResponse>>> Review(long id,ReviewProblemRequest request,CancellationToken ct)=>Ok(ApiResponse<ProblemDetailResponse>.Success(await service.ReviewProblemAsync(id,request,User.Identity!.Name!,ct),"Proposed problem reviewed successfully"));
}
