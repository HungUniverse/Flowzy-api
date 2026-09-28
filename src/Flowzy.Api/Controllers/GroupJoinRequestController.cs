using Flowzy.Service.Contracts;using Flowzy.Service.Groups;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Flowzy.Api.Controllers;
[ApiController,Authorize,Route("api/groups")]
public sealed class GroupJoinRequestController(IGroupMembershipService service):ControllerBase
{private string Email=>User.Identity!.Name!;
 [Authorize(Roles="STUDENT"),HttpGet("join-requests/me")]public async Task<ActionResult<ApiResponse<IReadOnlyList<JoinRequestResponse>>>> Mine(CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<JoinRequestResponse>>.Success(await service.JoinRequestsAsync(null,Email,ct),"My join requests retrieved successfully"));
 [HttpPost("{groupId:long}/join-requests")]public async Task<ActionResult<ApiResponse<JoinRequestResponse>>> Create(long groupId,CreateJoinRequest r,CancellationToken ct)=>Ok(ApiResponse<JoinRequestResponse>.Success(await service.JoinAsync(groupId,r,Email,ct),"Join request submitted successfully"));
 [HttpGet("{groupId:long}/join-requests")]public async Task<ActionResult<ApiResponse<IReadOnlyList<JoinRequestResponse>>>> Group(long groupId,CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<JoinRequestResponse>>.Success(await service.JoinRequestsAsync(groupId,Email,ct),"Join requests retrieved successfully"));
 [HttpPost("{groupId:long}/join-requests/{id:long}/approve"),HttpPost("join-requests/{id:long}/approve")]public Task<ActionResult<ApiResponse<object>>> Approve(long? groupId,long id,CancellationToken ct)=>Respond(groupId,id,"approve","Join request approved successfully",ct);
 [HttpPost("{groupId:long}/join-requests/{id:long}/reject"),HttpPost("join-requests/{id:long}/reject")]public Task<ActionResult<ApiResponse<object>>> Reject(long? groupId,long id,CancellationToken ct)=>Respond(groupId,id,"reject","Join request rejected successfully",ct);
 [HttpPost("{groupId:long}/join-requests/{id:long}/cancel"),HttpPost("join-requests/{id:long}/cancel")]public Task<ActionResult<ApiResponse<object>>> Cancel(long? groupId,long id,CancellationToken ct)=>Respond(groupId,id,"cancel","Join request cancelled successfully",ct);
 private async Task<ActionResult<ApiResponse<object>>> Respond(long? groupId,long id,string action,string message,CancellationToken ct){await service.RespondJoinAsync(groupId,id,action,Email,ct);return Ok(ApiResponse<object>.Success(null,message));}}
