using Flowzy.Service.Contracts;
using Flowzy.Service.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController,Authorize,Route("api/milestone-submissions")]
public sealed class MilestoneSubmissionController(IMilestoneSubmissionService service):ControllerBase
{
    private string Email=>User.Identity!.Name!;
    [HttpGet("{submissionId:long}")] public async Task<ActionResult<ApiResponse<MilestoneSubmissionResponse>>> Get(long submissionId,CancellationToken ct)=>Ok(ApiResponse<MilestoneSubmissionResponse>.Success(await service.GetSubmissionAsync(submissionId,Email,ct),"Milestone submission retrieved successfully"));
    [HttpGet("groups/{groupId:long}")] public async Task<ActionResult<ApiResponse<IReadOnlyList<MilestoneSubmissionResponse>>>> Group(long groupId,CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<MilestoneSubmissionResponse>>.Success(await service.GetGroupSubmissionsAsync(groupId,Email,ct),"Milestone submissions retrieved successfully"));
    [HttpGet("milestones/{milestoneId:long}")] public async Task<ActionResult<ApiResponse<IReadOnlyList<MilestoneSubmissionResponse>>>> Milestone(long milestoneId,CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<MilestoneSubmissionResponse>>.Success(await service.GetMilestoneSubmissionsAsync(milestoneId,Email,ct),"Milestone submissions retrieved successfully"));
}
