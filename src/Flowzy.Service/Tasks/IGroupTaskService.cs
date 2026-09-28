using Flowzy.Service.Contracts;

namespace Flowzy.Service.Tasks;

public interface IGroupTaskService
{
    Task<GroupTaskBoardResponse> GetBoard(long groupId, long? boardId, string? priority, long? assigneeStudentId, string? search, bool includeArchived, string email, CancellationToken ct);
    Task<TaskDetailResponse> Create(long groupId, CreateTaskRequest request, string email, CancellationToken ct);
    Task<TaskDetailResponse> Get(long groupId, long taskId, string email, CancellationToken ct);
    Task<TaskDetailResponse> Update(long groupId, long taskId, UpdateTaskRequest request, string email, CancellationToken ct);
    Task<TaskDetailResponse> ReplaceAssignees(long groupId, long taskId, ReplaceTaskAssigneesRequest request, string email, CancellationToken ct);
    Task<TaskDetailResponse> Move(long groupId, long taskId, MoveTaskRequest request, string email, CancellationToken ct);
    Task<TaskDetailResponse> Archive(long groupId, long taskId, string email, CancellationToken ct);
    Task<TaskDetailResponse> Restore(long groupId, long taskId, string email, CancellationToken ct);
    Task<ChecklistItemResponse> AddChecklistItem(long groupId, long taskId, CreateChecklistItemRequest request, string email, CancellationToken ct);
    Task<ChecklistItemResponse> UpdateChecklistItem(long groupId, long taskId, long itemId, UpdateChecklistItemRequest request, string email, CancellationToken ct);
    Task DeleteChecklistItem(long groupId, long taskId, long itemId, string email, CancellationToken ct);
    Task<PageResponse<TaskCommentResponse>> GetComments(long groupId, long taskId, int page, int size, string email, CancellationToken ct);
    Task<TaskCommentResponse> CreateComment(long groupId, long taskId, CreateTaskCommentRequest request, string email, CancellationToken ct);
    Task<TaskCommentResponse> UpdateComment(long groupId, long taskId, long commentId, UpdateTaskCommentRequest request, string email, CancellationToken ct);
    Task DeleteComment(long groupId, long taskId, long commentId, string email, CancellationToken ct);
    Task<PageResponse<TaskActivityResponse>> GetActivities(long groupId, long taskId, int page, int size, string email, CancellationToken ct);
    Task<PageResponse<TaskSummaryResponse>> GetMine(long? groupId, string? status, string? priority, bool? overdue, DateTime? dueBefore, int page, int size, string email, CancellationToken ct);
    Task Reorder(long groupId, ReorderTaskRequest request, string email, CancellationToken ct);
    Task HandleMemberRemoval(long groupId, long studentId, CancellationToken ct);
}
