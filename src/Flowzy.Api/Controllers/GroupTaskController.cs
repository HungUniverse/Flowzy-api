using Flowzy.Service.Contracts;
using Flowzy.Service.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize]
public sealed class GroupTaskController(IGroupTaskService service) : ControllerBase
{
    private string Email => User.Identity?.Name ?? throw new UnauthorizedAccessException("Not authenticated");

    [HttpGet("api/groups/{groupId:long}/board"), HttpGet("api/groups/{groupId:long}/boards/{pathBoardId:long}")]
    public async Task<ActionResult<ApiResponse<GroupTaskBoardResponse>>> Board(long groupId, long? pathBoardId,
        [FromQuery] long? boardId, [FromQuery] string? priority, [FromQuery] long? assigneeStudentId,
        [FromQuery] string? search, [FromQuery] bool includeArchived = false, CancellationToken ct = default) =>
        Ok(ApiResponse<GroupTaskBoardResponse>.Success(await service.GetBoard(groupId, pathBoardId ?? boardId, priority,
            assigneeStudentId, search, includeArchived, Email, ct), "Group task board retrieved successfully"));

    [HttpPost("api/groups/{groupId:long}/tasks")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Create(long groupId, CreateTaskRequest request, CancellationToken ct) =>
        StatusCode(201, ApiResponse<TaskDetailResponse>.Success(await service.Create(groupId, request, Email, ct), "Task created successfully"));
    [HttpGet("api/groups/{groupId:long}/tasks/{taskId:long}")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Get(long groupId, long taskId, CancellationToken ct) =>
        Ok(ApiResponse<TaskDetailResponse>.Success(await service.Get(groupId, taskId, Email, ct), "Task details retrieved successfully"));
    [HttpPatch("api/groups/{groupId:long}/tasks/{taskId:long}")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Update(long groupId, long taskId, UpdateTaskRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TaskDetailResponse>.Success(await service.Update(groupId, taskId, request, Email, ct), "Task updated successfully"));
    [HttpPut("api/groups/{groupId:long}/tasks/{taskId:long}/assignees")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Assignees(long groupId, long taskId, ReplaceTaskAssigneesRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TaskDetailResponse>.Success(await service.ReplaceAssignees(groupId, taskId, request, Email, ct), "Task assignees replaced successfully"));
    [HttpPatch("api/groups/{groupId:long}/tasks/{taskId:long}/move")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Move(long groupId, long taskId, MoveTaskRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TaskDetailResponse>.Success(await service.Move(groupId, taskId, request, Email, ct), "Task moved successfully"));
    [HttpDelete("api/groups/{groupId:long}/tasks/{taskId:long}")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Archive(long groupId, long taskId, CancellationToken ct) =>
        Ok(ApiResponse<TaskDetailResponse>.Success(await service.Archive(groupId, taskId, Email, ct), "Task archived successfully"));
    [HttpPost("api/groups/{groupId:long}/tasks/{taskId:long}/restore")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> Restore(long groupId, long taskId, CancellationToken ct) =>
        Ok(ApiResponse<TaskDetailResponse>.Success(await service.Restore(groupId, taskId, Email, ct), "Task restored successfully"));

    [HttpPost("api/groups/{groupId:long}/tasks/{taskId:long}/checklist-items")]
    public async Task<ActionResult<ApiResponse<ChecklistItemResponse>>> AddChecklist(long groupId, long taskId, CreateChecklistItemRequest request, CancellationToken ct) =>
        StatusCode(201, ApiResponse<ChecklistItemResponse>.Success(await service.AddChecklistItem(groupId, taskId, request, Email, ct), "Checklist item added successfully"));
    [HttpPatch("api/groups/{groupId:long}/tasks/{taskId:long}/checklist-items/{itemId:long}")]
    public async Task<ActionResult<ApiResponse<ChecklistItemResponse>>> UpdateChecklist(long groupId, long taskId, long itemId, UpdateChecklistItemRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ChecklistItemResponse>.Success(await service.UpdateChecklistItem(groupId, taskId, itemId, request, Email, ct), "Checklist item updated successfully"));
    [HttpDelete("api/groups/{groupId:long}/tasks/{taskId:long}/checklist-items/{itemId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteChecklist(long groupId, long taskId, long itemId, CancellationToken ct)
    { await service.DeleteChecklistItem(groupId, taskId, itemId, Email, ct); return Ok(ApiResponse<object>.Success(null, "Checklist item deleted successfully")); }

    [HttpGet("api/groups/{groupId:long}/tasks/{taskId:long}/comments")]
    public async Task<ActionResult<ApiResponse<PageResponse<TaskCommentResponse>>>> Comments(long groupId, long taskId, [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<TaskCommentResponse>>.Success(await service.GetComments(groupId, taskId, page, size, Email, ct), "Comments retrieved successfully"));
    [HttpPost("api/groups/{groupId:long}/tasks/{taskId:long}/comments")]
    public async Task<ActionResult<ApiResponse<TaskCommentResponse>>> AddComment(long groupId, long taskId, CreateTaskCommentRequest request, CancellationToken ct) =>
        StatusCode(201, ApiResponse<TaskCommentResponse>.Success(await service.CreateComment(groupId, taskId, request, Email, ct), "Comment added successfully"));
    [HttpPatch("api/groups/{groupId:long}/tasks/{taskId:long}/comments/{commentId:long}")]
    public async Task<ActionResult<ApiResponse<TaskCommentResponse>>> UpdateComment(long groupId, long taskId, long commentId, UpdateTaskCommentRequest request, CancellationToken ct) =>
        Ok(ApiResponse<TaskCommentResponse>.Success(await service.UpdateComment(groupId, taskId, commentId, request, Email, ct), "Comment updated successfully"));
    [HttpDelete("api/groups/{groupId:long}/tasks/{taskId:long}/comments/{commentId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteComment(long groupId, long taskId, long commentId, CancellationToken ct)
    { await service.DeleteComment(groupId, taskId, commentId, Email, ct); return Ok(ApiResponse<object>.Success(null, "Comment deleted successfully")); }
    [HttpGet("api/groups/{groupId:long}/tasks/{taskId:long}/activities")]
    public async Task<ActionResult<ApiResponse<PageResponse<TaskActivityResponse>>>> Activities(long groupId, long taskId, [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<TaskActivityResponse>>.Success(await service.GetActivities(groupId, taskId, page, size, Email, ct), "Activities retrieved successfully"));

    [HttpGet("api/tasks/me")]
    public async Task<ActionResult<ApiResponse<PageResponse<TaskSummaryResponse>>>> Mine([FromQuery] long? groupId,
        [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] bool? overdue, [FromQuery] DateTime? dueBefore,
        [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<TaskSummaryResponse>>.Success(await service.GetMine(groupId, status, priority, overdue, dueBefore, page, size, Email, ct), "My assigned tasks retrieved successfully"));
    [HttpPost("api/groups/{groupId:long}/tasks/reorder")]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(long groupId, ReorderTaskRequest request, CancellationToken ct)
    { await service.Reorder(groupId, request, Email, ct); return Ok(ApiResponse<object>.Success(null, "Task reordered successfully")); }
}
