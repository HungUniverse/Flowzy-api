using System.ComponentModel.DataAnnotations;

namespace Flowzy.Service.Contracts;

public sealed record CreateTaskRequest(
    [Required(ErrorMessage = "Title is required"), MaxLength(255, ErrorMessage = "Title must be at most 255 characters")] string Title,
    string? Description, string? Status, string? Priority, DateTime? DueAt,
    IReadOnlyList<long?>? AssigneeStudentIds, long? BoardId);

public sealed record UpdateTaskRequest(
    [MaxLength(255, ErrorMessage = "Title must be at most 255 characters")] string? Title,
    string? Description, string? Priority, DateTime? DueAt, bool? ClearDueAt,
    [Required(ErrorMessage = "Version is required for optimistic concurrency control")] long? Version);

public sealed record ReplaceTaskAssigneesRequest(
    [Required(ErrorMessage = "Assignee student IDs are required")] IReadOnlyList<long?>? AssigneeStudentIds,
    long? Version);

public sealed record MoveTaskRequest(
    [Required(ErrorMessage = "Status is required")] string? Status,
    [Required(ErrorMessage = "Position is required"), Range(0, long.MaxValue, ErrorMessage = "Position must be zero or greater")] long? Position,
    [Required(ErrorMessage = "Version is required")] long? Version);

public sealed record ReorderTaskRequest(
    [Required(ErrorMessage = "Task ID is required")] long? TaskId,
    [Required(ErrorMessage = "Target status is required")] string? TargetStatus,
    [Required(ErrorMessage = "Target index is required")] long? TargetIndex);

public sealed record CreateChecklistItemRequest(
    [Required(ErrorMessage = "Title must not be blank"), MaxLength(500, ErrorMessage = "Title must be at most 500 characters")] string Title);
public sealed record UpdateChecklistItemRequest(
    [MaxLength(500, ErrorMessage = "Title must be at most 500 characters")] string? Title,
    bool? Completed,
    [Range(0, int.MaxValue, ErrorMessage = "Position must be zero or greater")] int? Position);
public sealed record CreateTaskCommentRequest([Required(ErrorMessage = "Comment content must not be blank")] string Content);
public sealed record UpdateTaskCommentRequest([Required(ErrorMessage = "Comment content must not be blank")] string Content);

public sealed record TaskAssigneeResponse(long StudentId, string StudentCode, string FullName, string Email);
public sealed record ChecklistItemResponse(long Id, string Title, bool Completed, long? CompletedByAccountId, DateTime? CompletedAt, long Position);
public sealed record TaskSummaryResponse(long Id, string Title, string Status, string Priority, DateTime? DueAt,
    long Position, long Version, long? CreatedByStudentId, string? CreatedByStudentName, DateTime? ArchivedAt,
    DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<TaskAssigneeResponse> Assignees,
    int ChecklistCount, int ChecklistCompletedCount, int ChecklistProgressPercent, bool Overdue);
public sealed record TaskDetailResponse(long Id, string Title, string? Description, string Status, string Priority,
    DateTime? DueAt, long Position, long Version, long? CreatedByStudentId, string? CreatedByStudentName,
    DateTime? ArchivedAt, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<TaskAssigneeResponse> Assignees,
    int ChecklistCount, int ChecklistCompletedCount, int ChecklistProgressPercent, bool Overdue,
    IReadOnlyList<ChecklistItemResponse> ChecklistItems);
public sealed record BoardColumnResponse(string Status, int DisplayOrder, IReadOnlyList<TaskSummaryResponse> Tasks);
public sealed record GroupTaskBoardResponse(long GroupId, string GroupName, IReadOnlyList<BoardColumnResponse> Columns,
    int ActiveTaskCount, int OverdueTaskCount);
public sealed record TaskCommentResponse(long Id, long AuthorAccountId, string AuthorEmail, string AuthorFullName,
    string AuthorRole, string Content, DateTime? EditedAt, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record ActivityActorResponse(long Id, string Email, string FullName, string Role);
public sealed record TaskActivityResponse(long Id, long TaskId, long GroupId, ActivityActorResponse? Actor,
    string ActivityType, IReadOnlyDictionary<string, object?> Details, DateTime CreatedAt);
