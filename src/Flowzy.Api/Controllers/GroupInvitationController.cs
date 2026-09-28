using Flowzy.Service.Contracts;using Flowzy.Service.Groups;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Flowzy.Api.Controllers;
[ApiController,Authorize,Route("api/groups")]
public sealed class GroupInvitationController(IGroupMembershipService service):ControllerBase
{private string Email=>User.Identity!.Name!;
 [HttpPost("{groupId:long}/invitations")]public async Task<ActionResult<ApiResponse<InvitationResponse>>> Create(long groupId,CreateInvitationRequest r,CancellationToken ct)=>Ok(ApiResponse<InvitationResponse>.Success(await service.InviteAsync(groupId,r,Email,ct),"Invitation created successfully"));
 [HttpGet("{groupId:long}/invitations")]public async Task<ActionResult<ApiResponse<IReadOnlyList<InvitationResponse>>>> Group(long groupId,CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<InvitationResponse>>.Success(await service.InvitationsAsync(groupId,Email,ct),"Invitations retrieved successfully"));
 [Authorize(Roles="STUDENT"),HttpGet("invitations/me")]public async Task<ActionResult<ApiResponse<IReadOnlyList<InvitationResponse>>>> Mine(CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<InvitationResponse>>.Success(await service.InvitationsAsync(null,Email,ct),"Pending invitations retrieved successfully"));
 [Authorize(Roles="STUDENT"),HttpPost("invitations/{id:long}/accept")]public Task<ActionResult<ApiResponse<object>>> Accept(long id,CancellationToken ct)=>Respond(id,"accept","Invitation accepted successfully",ct);
 [Authorize(Roles="STUDENT"),HttpPost("invitations/{id:long}/decline")]public Task<ActionResult<ApiResponse<object>>> Decline(long id,CancellationToken ct)=>Respond(id,"decline","Invitation declined successfully",ct);
 [HttpPost("invitations/{id:long}/cancel")]public Task<ActionResult<ApiResponse<object>>> Cancel(long id,CancellationToken ct)=>Respond(id,"cancel","Invitation cancelled successfully",ct);
 private async Task<ActionResult<ApiResponse<object>>> Respond(long id,string action,string message,CancellationToken ct){await service.RespondInvitationAsync(id,action,Email,ct);return Ok(ApiResponse<object>.Success(null,message));}}
