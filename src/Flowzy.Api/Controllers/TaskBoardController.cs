using Flowzy.Service.Contracts;
using Flowzy.Service.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController,Authorize,Route("api/groups/{groupId:long}")]
public sealed class TaskBoardController(ITaskBoardService service):ControllerBase
{
    private string Email=>User.Identity!.Name!;
    [HttpGet("boards"),HttpGet("task-boards")] public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskBoardResponse>>>> List(long groupId,CancellationToken ct)=>Ok(ApiResponse<IReadOnlyList<TaskBoardResponse>>.Success(await service.GetBoardsAsync(groupId,Email,ct),"Task boards retrieved successfully"));
    [HttpPost("boards"),HttpPost("task-boards")] public async Task<ActionResult<ApiResponse<TaskBoardResponse>>> Create(long groupId,CreateTaskBoardRequest request,CancellationToken ct)=>StatusCode(201,ApiResponse<TaskBoardResponse>.Success(await service.CreateBoardAsync(groupId,request,Email,ct),"Task board created successfully"));
    [HttpPatch("boards/{boardId:long}"),HttpPatch("task-boards/{boardId:long}")] public async Task<ActionResult<ApiResponse<TaskBoardResponse>>> Update(long groupId,long boardId,UpdateTaskBoardRequest request,CancellationToken ct)=>Ok(ApiResponse<TaskBoardResponse>.Success(await service.UpdateBoardAsync(groupId,boardId,request,Email,ct),"Task board updated successfully"));
    [HttpDelete("boards/{boardId:long}"),HttpDelete("task-boards/{boardId:long}")] public async Task<ActionResult<ApiResponse<object>>> Delete(long groupId,long boardId,CancellationToken ct){await service.DeleteBoardAsync(groupId,boardId,Email,ct);return Ok(ApiResponse<object>.Success(null,"Task board deleted successfully"));}
}
