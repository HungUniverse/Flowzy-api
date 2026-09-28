using Flowzy.Service.Contracts;
using Flowzy.Service.Timelines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController,Authorize,Flowzy.Api.Filters.InstructorTimelineAlias,Route("api/course-milestones")]
[Route("api/instructor/milestones")]
public sealed class CourseMilestoneController(ICourseMilestoneService service):ControllerBase
{
    private string Email=>User.Identity!.Name!;
    [HttpPost] public async Task<ActionResult<ApiResponse<CourseMilestoneResponse>>> Create(CourseMilestoneRequest request,CancellationToken ct)=>Ok(ApiResponse<CourseMilestoneResponse>.Success(await service.CreateMilestoneAsync(request,Email,ct),"Course milestone created successfully"));
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<CourseMilestoneResponse>>>> List([FromQuery]string? term,[FromQuery]string? courseCode,CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<CourseMilestoneResponse>>.Success(await service.GetMilestonesAsync(term,courseCode,Email,ct),"Course milestones retrieved successfully"));
    [HttpGet("{id:long}")] public async Task<ActionResult<ApiResponse<CourseMilestoneResponse>>> Get(long id,CancellationToken ct)=>Ok(ApiResponse<CourseMilestoneResponse>.Success(await service.GetMilestoneAsync(id,Email,ct),"Course milestone retrieved successfully"));
    [AcceptVerbs("PUT","PATCH"),Route("{id:long}")] public async Task<ActionResult<ApiResponse<CourseMilestoneResponse>>> Update(long id,UpdateCourseMilestoneRequest request,CancellationToken ct)=>Ok(ApiResponse<CourseMilestoneResponse>.Success(await service.UpdateMilestoneAsync(id,request,Email,ct),"Course milestone updated successfully"));
    [HttpDelete("{id:long}")] public async Task<ActionResult<ApiResponse<object>>> Delete(long id,CancellationToken ct){await service.DeleteMilestoneAsync(id,Email,ct);return Ok(ApiResponse<object>.Success(null,"Course milestone deleted successfully"));}
    [AcceptVerbs("GET","POST"),Route("{milestoneId:long}/outcomes")] public ActionResult<ApiResponse<object>> Outcomes()=>StatusCode(410,ApiResponse<object>.Error(410,"Outcome types were removed; use a generically named timeline milestone"));
}
