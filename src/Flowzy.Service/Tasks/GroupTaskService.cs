using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Service.Tasks;

public sealed class GroupTaskService(FlowzyDbContext db) : IGroupTaskService
{
    private static readonly string[] Statuses = ["BACKLOG", "TODO", "IN_PROGRESS", "REVIEW", "DONE"];
    private static readonly string[] Priorities = ["LOW", "MEDIUM", "HIGH", "URGENT"];
    private const string Stale = "The resource has been modified by another transaction. Please reload and try again.";
    private static DateTime Now => DateTime.UtcNow;

    public async Task<GroupTaskBoardResponse> GetBoard(long groupId, long? boardId, string? priority,
        long? assigneeStudentId, string? search, bool includeArchived, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var group = await ReadAccess(groupId, email, ct);
        var board = await ResolveBoard(group, boardId, ct);
        if (board.ArchivedAt is not null) throw new BadRequestException("Task board is archived");
        var p = ParsePriority(priority, "Invalid priority filter", true);
        var query = TaskQuery().Where(x => x.GroupId == groupId && x.BoardId == board.Id);
        if (!includeArchived) query = query.Where(x => x.ArchivedAt == null);
        if (p is not null) query = query.Where(x => x.Priority == p);
        if (assigneeStudentId is not null) query = query.Where(x => x.TaskAssignees.Any(a => a.StudentId == assigneeStudentId));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(q) || (x.Description != null && x.Description.ToLower().Contains(q)));
        }
        var tasks = await query.OrderBy(x => x.Status).ThenBy(x => x.Position).ThenBy(x => x.Id).ToListAsync(ct);
        var now = Now;
        var summaries = tasks.Select(x => Summary(x, now)).ToList();
        var columns = Statuses.Select((status, index) => new BoardColumnResponse(status, index,
            summaries.Where(x => x.Status == status).OrderBy(x => x.Position).ToList())).ToList();
        var active = await db.GroupTasks.CountAsync(x => x.GroupId == groupId && x.BoardId == board.Id && x.ArchivedAt == null, ct);
        var overdue = await db.GroupTasks.CountAsync(x => x.GroupId == groupId && x.BoardId == board.Id && x.ArchivedAt == null &&
            x.Status != "DONE" && x.DueAt != null && x.DueAt < now, ct);
        await tx.CommitAsync(ct);
        return new(group.Id, group.Name, columns, active, overdue);
    }

    public async Task<TaskDetailResponse> Create(long groupId, CreateTaskRequest r, string email, CancellationToken ct)
    {
        var title = Required(r.Title, "Title is required");
        if (title.Length > 255) throw new BadRequestException("Title must be at most 255 characters");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (group, caller) = await Leader(groupId, email, "Only the group leader is authorized to create tasks", ct);
        var board = await ResolveBoard(group, r.BoardId, ct);
        if (board.ArchivedAt is not null) throw new BadRequestException("Cannot add task to an archived board");
        var status = string.IsNullOrWhiteSpace(r.Status) ? "BACKLOG" : r.Status.Trim();
        if (status is not ("BACKLOG" or "TODO")) throw new BadRequestException("Initial task status must be either BACKLOG or TODO");
        var priority = ParsePriority(r.Priority, "Invalid priority value", true) ?? "MEDIUM";
        Future(r.DueAt);
        var assignees = await ValidateAssignees(groupId, r.AssigneeStudentIds ?? [], ct);
        var max = await db.GroupTasks.Where(x => x.BoardId == board.Id && x.Status == status && x.ArchivedAt == null).MaxAsync(x => (long?)x.Position, ct) ?? -1;
        var now = Now;
        var task = new GroupTask { GroupId = groupId, BoardId = board.Id, Title = title, Description = r.Description,
            Status = status, Priority = priority, DueAt = Utc(r.DueAt), Position = max + 1, CreatedByStudentId = caller.Id,
            Version = 0, CreatedAt = now, UpdatedAt = now };
        db.GroupTasks.Add(task);
        await db.SaveChangesAsync(ct);
        foreach (var student in assignees)
            db.TaskAssignees.Add(new TaskAssignee { TaskId = task.Id, StudentId = student.Id, AssignedByStudentId = caller.Id, AssignedAt = now });
        Activity(task, caller.AccountId, "TASK_CREATED", new { title = task.Title, status = task.Status });
        if (assignees.Count != 0)
        {
            var ids = assignees.Select(x => x.Id).ToList();
            Activity(task, caller.AccountId, "ASSIGNEES_CHANGED", new { added = ids, removed = Array.Empty<long>() });
            await Notify(assignees.Select(x => x.AccountId), null, "TASK_ASSIGNED", "New Task Assigned",
                $"You have been assigned to task '{task.Title}' in group {group.Name}.", task, $"TASK_ASSIGNED:{task.Id}:{task.Version}", ct);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Detail(await DetailedTask(groupId, task.Id, ct), Now);
    }

    public async Task<TaskDetailResponse> Get(long groupId, long taskId, string email, CancellationToken ct)
    {
        await ReadAccess(groupId, email, ct);
        return Detail(await DetailedTask(groupId, taskId, ct), Now);
    }

    public async Task<TaskDetailResponse> Update(long groupId, long taskId, UpdateTaskRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (group, caller) = await Leader(groupId, email, "Only the group leader is authorized to update tasks", ct);
        var task = await db.GroupTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        Mutable(task); Version(task, r.Version);
        var changes = new Dictionary<string, object?>();
        if (r.Title is not null)
        {
            var title = r.Title.Trim(); if (title.Length == 0) throw new BadRequestException("Title cannot be blank");
            if (title.Length > 255) throw new BadRequestException("Title must be at most 255 characters");
            if (task.Title != title) { changes["title"] = Change(task.Title, title); task.Title = title; }
        }
        if (r.Description is not null && task.Description != r.Description) { changes["description"] = Change(task.Description ?? "", r.Description); task.Description = r.Description; }
        if (!string.IsNullOrWhiteSpace(r.Priority))
        {
            var priority = ParsePriority(r.Priority, "Invalid priority value")!;
            if (task.Priority != priority) { changes["priority"] = Change(task.Priority, priority); task.Priority = priority; }
        }
        if (r.ClearDueAt == true && r.DueAt is not null) throw new BadRequestException("dueAt and clearDueAt cannot be supplied together");
        if (r.ClearDueAt == true && task.DueAt is not null) { changes["dueAt"] = Change(task.DueAt.Value.ToUniversalTime().ToString("O"), ""); task.DueAt = null; }
        else if (r.DueAt is not null && task.DueAt != Utc(r.DueAt)) { Future(r.DueAt); changes["dueAt"] = Change(task.DueAt?.ToUniversalTime().ToString("O") ?? "", Utc(r.DueAt)!.Value.ToString("O")); task.DueAt = Utc(r.DueAt); }
        if (changes.Count != 0) { Touch(task); Activity(task, caller.AccountId, "TASK_UPDATED", new { changes }); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
        return Detail(await DetailedTask(groupId, taskId, ct), Now);
    }

    public async Task<TaskDetailResponse> ReplaceAssignees(long groupId, long taskId, ReplaceTaskAssigneesRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (group, caller) = await Leader(groupId, email, "Only the group leader is authorized to assign tasks", ct);
        var task = await db.GroupTasks.Include(x => x.TaskAssignees).FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        Mutable(task); if (r.Version is null) throw new BadRequestException("version is required for optimistic concurrency control"); Version(task, r.Version);
        if (r.AssigneeStudentIds is null) throw new BadRequestException("assigneeStudentIds is required");
        var students = await ValidateAssignees(groupId, r.AssigneeStudentIds, ct);
        var wanted = students.Select(x => x.Id).ToHashSet(); var old = task.TaskAssignees.Select(x => x.StudentId).ToList();
        var added = wanted.Except(old).ToList(); var removed = old.Except(wanted).ToList();
        if (added.Count != 0 || removed.Count != 0)
        {
            db.TaskAssignees.RemoveRange(task.TaskAssignees.Where(x => removed.Contains(x.StudentId)));
            foreach (var id in added) db.TaskAssignees.Add(new TaskAssignee { TaskId = taskId, StudentId = id, AssignedByStudentId = caller.Id, AssignedAt = Now });
            Touch(task); Activity(task, caller.AccountId, "ASSIGNEES_CHANGED", new { added, removed });
            await Notify(students.Where(x => added.Contains(x.Id)).Select(x => x.AccountId), caller.AccountId, "TASK_ASSIGNED", "New Task Assignment",
                $"You have been assigned to task '{task.Title}' in group {group.Name}.", task, $"TASK_ASSIGNED:{taskId}:{string.Join(',', added)}", ct);
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return Detail(await DetailedTask(groupId, taskId, ct), Now);
    }

    public async Task<TaskDetailResponse> Move(long groupId, long taskId, MoveTaskRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var group = await LockedGroup(groupId, ct); Writable(group);
        var caller = await Student(email, "Student profile not found", ct);
        var task = await db.GroupTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        Mutable(task); Version(task, r.Version);
        var target = ParseStatus(r.Status, "Target status is required", "Invalid target status");
        if (r.Position is null || r.Position < 0) throw new BadRequestException("Target position must be zero or greater");
        var leader = group.LeaderStudentId == caller.Id;
        var member = await db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == caller.Id, ct);
        var assignee = member && await db.TaskAssignees.AnyAsync(x => x.TaskId == taskId && x.StudentId == caller.Id, ct);
        if (task.Status == target && !leader) throw new ForbiddenException("Only the group leader can reorder tasks");
        if (task.Status != target && !leader && !assignee) throw new ForbiddenException("Only the group leader or task assignees can update task status");
        var board = await EnsureTaskBoard(task, group, ct); var oldStatus = task.Status; var oldPosition = task.Position; var oldVersion = task.Version;
        var locked = await db.GroupTasks.FromSqlInterpolated($"SELECT * FROM group_tasks WHERE board_id={board.Id} AND archived_at IS NULL AND status IN ({oldStatus},{target}) FOR UPDATE").ToListAsync(ct);
        if (oldStatus == target)
        {
            var col = locked.Where(x => x.Status == oldStatus).OrderBy(x => x.Position).ThenBy(x => x.Id).ToList();
            if (!col.Remove(task)) throw new ConflictException("Task is no longer in the expected column. Please reload and try again.");
            col.Insert(Math.Min((int)Math.Min(r.Position.Value, int.MaxValue), col.Count), task); Normalize(col);
        }
        else
        {
            var source = locked.Where(x => x.Status == oldStatus).OrderBy(x => x.Position).ThenBy(x => x.Id).ToList();
            var destination = locked.Where(x => x.Status == target).OrderBy(x => x.Position).ThenBy(x => x.Id).ToList();
            if (!source.Remove(task)) throw new ConflictException("Task is no longer in the expected column. Please reload and try again.");
            task.Status = target; destination.Insert(Math.Min((int)Math.Min(r.Position.Value, int.MaxValue), destination.Count), task);
            Normalize(source); Normalize(destination);
        }
        if (task.Version == oldVersion && (oldStatus != task.Status || oldPosition != task.Position)) Touch(task);
        Activity(task, caller.AccountId, "TASK_MOVED", new { previousStatus = oldStatus, newStatus = target, previousIndex = oldPosition, newIndex = task.Position });
        if (oldStatus != target)
        {
            var recipients = await LeaderAndAssigneeAccounts(taskId, group.LeaderStudentId, ct);
            await Notify(recipients, caller.AccountId, "TASK_STATUS_CHANGED", "Task Status Updated", $"Task '{task.Title}' status changed from {oldStatus} to {target}.", task, $"TASK_STATUS_CHANGED:{task.Id}:{task.Version}", ct,
                extraParams: new() { ["oldStatus"] = oldStatus, ["newStatus"] = target });
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Detail(await DetailedTask(groupId, taskId, ct), Now);
    }

    public Task<TaskDetailResponse> Archive(long groupId, long taskId, string email, CancellationToken ct) => SetArchived(groupId, taskId, email, true, ct);
    public Task<TaskDetailResponse> Restore(long groupId, long taskId, string email, CancellationToken ct) => SetArchived(groupId, taskId, email, false, ct);

    private async Task<TaskDetailResponse> SetArchived(long groupId, long taskId, string email, bool archive, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (group, caller) = await Leader(groupId, email, archive ? "Only the group leader is authorized to archive tasks" : "Only the group leader is authorized to restore tasks", ct);
        var task = await db.GroupTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        if (archive && task.ArchivedAt is not null) throw new BadRequestException("Task is already archived");
        if (!archive && task.ArchivedAt is null) throw new BadRequestException("Task is not archived");
        var board = await EnsureTaskBoard(task, group, ct);
        var active = await db.GroupTasks.FromSqlInterpolated($"SELECT * FROM group_tasks WHERE board_id={board.Id} AND status={task.Status} AND archived_at IS NULL FOR UPDATE").OrderBy(x => x.Position).ThenBy(x => x.Id).ToListAsync(ct);
        var oldVersion = task.Version;
        if (archive)
        {
            task.ArchivedAt = Now; Touch(task); Normalize(active.Where(x => x.Id != task.Id).ToList());
            Activity(task, caller.AccountId, "TASK_ARCHIVED", new { archivedAt = task.ArchivedAt.Value.ToUniversalTime().ToString("O") });
        }
        else
        {
            task.ArchivedAt = null; task.Position = active.Count; active.Add(task); Normalize(active); if (task.Version == oldVersion) Touch(task);
            var memberships = await db.StudentGroupMembers.Where(x => x.GroupId == groupId).Select(x => x.StudentId).ToListAsync(ct);
            var stale = await db.TaskAssignees.Where(x => x.TaskId == taskId && !memberships.Contains(x.StudentId)).ToListAsync(ct);
            foreach (var assignment in stale) { db.TaskAssignees.Remove(assignment); Activity(task, null, "ASSIGNEE_REMOVED_FROM_GROUP", new { studentId = assignment.StudentId }); }
            Activity(task, caller.AccountId, "TASK_RESTORED", new { restoredAt = Now.ToString("O") });
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Detail(await DetailedTask(groupId, taskId, ct), Now);
    }

    public async Task<ChecklistItemResponse> AddChecklistItem(long groupId, long taskId, CreateChecklistItemRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (task, caller) = await ChecklistMutation(groupId, taskId, email, ct);
        var title = Required(r.Title, "Title is required"); if (title.Length > 500) throw new BadRequestException("Title must be at most 500 characters");
        var pos = (await db.TaskChecklistItems.Where(x => x.TaskId == taskId).MaxAsync(x => (long?)x.Position, ct) ?? -1) + 1;
        var now = Now; var item = new TaskChecklistItem { TaskId = taskId, Title = title, Completed = false, Position = pos, CreatedAt = now, UpdatedAt = now };
        db.TaskChecklistItems.Add(item); await db.SaveChangesAsync(ct);
        Activity(task, caller.AccountId, "CHECKLIST_ITEM_CREATED", new { id = item.Id, title = item.Title });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Checklist(item);
    }

    public async Task<ChecklistItemResponse> UpdateChecklistItem(long groupId, long taskId, long itemId, UpdateChecklistItemRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (task, caller) = await ChecklistMutation(groupId, taskId, email, ct);
        var item = await db.TaskChecklistItems.FirstOrDefaultAsync(x => x.Id == itemId && x.TaskId == taskId, ct) ?? throw new NotFoundException("Checklist item not found");
        var changes = new Dictionary<string, object?>();
        if (r.Title is not null)
        {
            var title = Required(r.Title, "Title is required"); if (title.Length > 500) throw new BadRequestException("Title must be at most 500 characters");
            if (item.Title != title) { changes["title"] = Change(item.Title, title); item.Title = title; }
        }
        if (r.Completed is not null && item.Completed != r.Completed.Value)
        {
            changes["completed"] = Change(item.Completed, r.Completed.Value); item.Completed = r.Completed.Value;
            item.CompletedByAccountId = item.Completed ? caller.AccountId : null; item.CompletedAt = item.Completed ? Now : null;
        }
        item.UpdatedAt = Now;
        if (r.Position is not null)
        {
            var items = await db.TaskChecklistItems.Where(x => x.TaskId == taskId).OrderBy(x => x.Position).ThenBy(x => x.Id).ToListAsync(ct);
            var old = items.FindIndex(x => x.Id == itemId); var target = Math.Min(r.Position.Value, items.Count - 1);
            if (old >= 0 && old != target) { changes["position"] = Change(old, target); items.RemoveAt(old); items.Insert(target, item); for (var i = 0; i < items.Count; i++) { items[i].Position = i; items[i].UpdatedAt = Now; } }
        }
        if (changes.Count != 0) Activity(task, caller.AccountId, "CHECKLIST_ITEM_UPDATED", new { id = item.Id, changes });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Checklist(item);
    }

    public async Task DeleteChecklistItem(long groupId, long taskId, long itemId, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (task, caller) = await ChecklistMutation(groupId, taskId, email, ct);
        var item = await db.TaskChecklistItems.FirstOrDefaultAsync(x => x.Id == itemId && x.TaskId == taskId, ct) ?? throw new NotFoundException("Checklist item not found");
        var title = item.Title; db.TaskChecklistItems.Remove(item); await db.SaveChangesAsync(ct);
        var rest = await db.TaskChecklistItems.Where(x => x.TaskId == taskId).OrderBy(x => x.Position).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rest.Count; i++) { rest[i].Position = i; rest[i].UpdatedAt = Now; }
        Activity(task, caller.AccountId, "CHECKLIST_ITEM_DELETED", new { id = itemId, title });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<PageResponse<TaskCommentResponse>> GetComments(long groupId, long taskId, int page, int size, string email, CancellationToken ct)
    {
        await CommentAccess(groupId, taskId, email, false, ct); Page(page, size); size = Math.Min(size, 100);
        var query = CommentQuery().Where(x => x.TaskId == taskId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
        var total = await query.LongCountAsync(ct); var rows = await query.Skip(page * size).Take(size).ToListAsync(ct);
        return PageResponse<TaskCommentResponse>.Create(rows.Select(Comment).ToList(), page, size, total);
    }

    public async Task<TaskCommentResponse> CreateComment(long groupId, long taskId, CreateTaskCommentRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (task, caller) = await CommentAccess(groupId, taskId, email, true, ct); Mutable(task);
        var content = Required(r.Content, "Content is required"); var now = Now;
        var comment = new TaskComment { TaskId = taskId, AuthorAccountId = caller.Id, Content = content, CreatedAt = now, UpdatedAt = now };
        db.TaskComments.Add(comment); await db.SaveChangesAsync(ct); Activity(task, caller.Id, "COMMENT_CREATED", new { id = comment.Id });
        var recipients = await LeaderAndAssigneeAccounts(taskId, task.Group.LeaderStudentId, ct);
        var authorName = caller.Student?.FullName ?? caller.Mentor?.FullName ?? caller.Instructor?.FullName ?? "Someone";
        await Notify(recipients, caller.Id, "TASK_COMMENT_CREATED", "New Task Comment", $"{authorName} commented on task '{task.Title}'.", task, $"TASK_COMMENT_CREATED:{comment.Id}", ct, comment.Id);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Comment(await CommentQuery().FirstAsync(x => x.Id == comment.Id, ct));
    }

    public async Task<TaskCommentResponse> UpdateComment(long groupId, long taskId, long commentId, UpdateTaskCommentRequest r, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (task, caller) = await CommentAccess(groupId, taskId, email, true, ct); Mutable(task);
        var comment = await db.TaskComments.FirstOrDefaultAsync(x => x.Id == commentId && x.TaskId == taskId, ct) ?? throw new NotFoundException("Comment not found");
        if (comment.AuthorAccountId != caller.Id) throw new ForbiddenException("Only the author can edit this comment");
        var content = Required(r.Content, "Content is required");
        if (comment.Content != content) { comment.Content = content; comment.EditedAt = Now; comment.UpdatedAt = Now; Activity(task, caller.Id, "COMMENT_UPDATED", new { id = comment.Id }); await db.SaveChangesAsync(ct); }
        await tx.CommitAsync(ct);
        return Comment(await CommentQuery().FirstAsync(x => x.Id == comment.Id, ct));
    }

    public async Task DeleteComment(long groupId, long taskId, long commentId, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var (task, caller) = await CommentAccess(groupId, taskId, email, true, ct); Mutable(task);
        var comment = await db.TaskComments.FirstOrDefaultAsync(x => x.Id == commentId && x.TaskId == taskId, ct) ?? throw new NotFoundException("Comment not found");
        var studentId = caller.Student?.Id ?? await db.Students.Where(x => x.AccountId == caller.Id).Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
        if (comment.AuthorAccountId != caller.Id && !(caller.Role == "STUDENT" && studentId is not null && studentId == task.Group.LeaderStudentId)) throw new ForbiddenException("Only the author or the group leader can delete this comment");
        db.TaskComments.Remove(comment); Activity(task, caller.Id, "COMMENT_DELETED", new { id = commentId }); await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<PageResponse<TaskActivityResponse>> GetActivities(long groupId, long taskId, int page, int size, string email, CancellationToken ct)
    {
        await CommentAccess(groupId, taskId, email, false, ct); Page(page, size); size = Math.Min(size, 100);
        var query = ActivityQuery().Where(x => x.TaskId == taskId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
        var total = await query.LongCountAsync(ct); var rows = await query.Skip(page * size).Take(size).ToListAsync(ct);
        return PageResponse<TaskActivityResponse>.Create(rows.Select(ActivityResponse).ToList(), page, size, total);
    }

    public async Task<PageResponse<TaskSummaryResponse>> GetMine(long? groupId, string? status, string? priority, bool? overdue,
        DateTime? dueBefore, int page, int size, string email, CancellationToken ct)
    {
        var student = await Student(email, "Only students can access their assigned tasks", ct); Page(page, size); size = Math.Min(size, 100);
        var s = string.IsNullOrWhiteSpace(status) ? null : ParseStatus(status, "Invalid status filter", "Invalid status filter");
        var p = ParsePriority(priority, "Invalid priority filter", true); var now = Now;
        var query = TaskQuery().Where(x => x.ArchivedAt == null && x.TaskAssignees.Any(a => a.StudentId == student.Id));
        if (groupId is not null) query = query.Where(x => x.GroupId == groupId); if (s is not null) query = query.Where(x => x.Status == s);
        if (p is not null) query = query.Where(x => x.Priority == p); if (dueBefore is not null) { var due = Utc(dueBefore); query = query.Where(x => x.DueAt != null && x.DueAt < due); }
        if (overdue == true) query = query.Where(x => x.Status != "DONE" && x.DueAt != null && x.DueAt < now);
        else if (overdue == false) query = query.Where(x => x.Status == "DONE" || x.DueAt == null || x.DueAt >= now);
        var total = await query.LongCountAsync(ct); var rows = await query.OrderBy(x => x.DueAt == null).ThenBy(x => x.DueAt).ThenByDescending(x => x.UpdatedAt).Skip(page * size).Take(size).ToListAsync(ct);
        return PageResponse<TaskSummaryResponse>.Create(rows.Select(x => Summary(x, now)).ToList(), page, size, total);
    }

    public async Task Reorder(long groupId, ReorderTaskRequest r, string email, CancellationToken ct)
    {
        if (r.TaskId is null) throw new BadRequestException("Task ID is required");
        var task = await db.GroupTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.TaskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        await Move(groupId, task.Id, new MoveTaskRequest(r.TargetStatus, r.TargetIndex, task.Version), email, ct);
    }

    public async Task HandleMemberRemoval(long groupId, long studentId, CancellationToken ct)
    {
        // Join the membership transaction so task cleanup and membership removal are atomic.
        await using var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        _ = await LockedGroup(groupId, ct);
        var assignments = await db.TaskAssignees.Include(x => x.Task).Where(x => x.StudentId == studentId && x.Task.GroupId == groupId && x.Task.ArchivedAt == null).ToListAsync(ct);
        foreach (var a in assignments) { db.TaskAssignees.Remove(a); Activity(a.Task, null, "ASSIGNEE_REMOVED_FROM_GROUP", new { studentId }); }
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
    }

    private IQueryable<GroupTask> TaskQuery() => db.GroupTasks.AsNoTracking()
        .Include(x => x.CreatedByStudent)
        .Include(x => x.TaskAssignees).ThenInclude(x => x.Student).ThenInclude(x => x.Account)
        .Include(x => x.TaskChecklistItems);

    private async Task<GroupTask> DetailedTask(long groupId, long taskId, CancellationToken ct) =>
        await TaskQuery().FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");

    private async Task<StudentGroup> ReadAccess(long groupId, string email, CancellationToken ct)
    {
        var account = await Account(email, ct);
        var group = await db.StudentGroups.Include(x => x.Mentor).FirstOrDefaultAsync(x => x.Id == groupId, ct) ?? throw new NotFoundException("Group not found");
        if (account.Role == "STUDENT")
        {
            var student = await db.Students.FirstOrDefaultAsync(x => x.AccountId == account.Id, ct) ?? throw new ForbiddenException("Student profile not found");
            if (!await db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == student.Id, ct)) throw new ForbiddenException("You are not a member of this student group");
        }
        else if (account.Role == "MENTOR")
        {
            var mentor = await db.Mentors.FirstOrDefaultAsync(x => x.AccountId == account.Id, ct) ?? throw new ForbiddenException("Mentor profile not found");
            if (group.MentorId != mentor.Id) throw new ForbiddenException("You are not the assigned mentor for this group");
        }
        else throw new ForbiddenException("Access denied");
        return group;
    }

    private async Task<(StudentGroup Group, Student Student)> Leader(long groupId, string email, string message, CancellationToken ct)
    {
        var group = await LockedGroup(groupId, ct); Writable(group); var student = await Student(email, "Student profile not found", ct);
        if (group.LeaderStudentId != student.Id) throw new ForbiddenException(message); return (group, student);
    }

    private async Task<StudentGroup> LockedGroup(long groupId, CancellationToken ct)
    {
        var group = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={groupId} FOR UPDATE").FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Group not found");
        await db.Entry(group).Reference(x => x.TermNavigation).LoadAsync(ct); return group;
    }

    private async Task<TaskBoard> ResolveBoard(StudentGroup group, long? boardId, CancellationToken ct)
    {
        if (boardId is not null) return await db.TaskBoards.FirstOrDefaultAsync(x => x.Id == boardId && x.GroupId == group.Id, ct) ?? throw new NotFoundException("Task board not found");
        var board = await db.TaskBoards.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.GroupId == group.Id && x.DefaultBoard, ct);
        if (board is not null) return board;
        if (db.Database.IsRelational())
        {
            group = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={group.Id} FOR UPDATE").FirstAsync(ct);
            board = await db.TaskBoards.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.GroupId == group.Id && x.DefaultBoard, ct);
            if (board is not null) return board;
        }
        var now = Now; board = new TaskBoard { GroupId = group.Id, Name = "Default", Description = "Default Board", Position = 0, DefaultBoard = true, CreatedAt = now, UpdatedAt = now };
        db.TaskBoards.Add(board); await db.SaveChangesAsync(ct); return board;
    }

    private async Task<TaskBoard> EnsureTaskBoard(GroupTask task, StudentGroup group, CancellationToken ct)
    {
        if (task.BoardId != 0) return await db.TaskBoards.FindAsync([task.BoardId], ct) ?? throw new NotFoundException("Task board not found");
        var board = await ResolveBoard(group, null, ct); task.BoardId = board.Id; Touch(task); return board;
    }

    private async Task<IReadOnlyList<Student>> ValidateAssignees(long groupId, IReadOnlyList<long?> ids, CancellationToken ct)
    {
        if (ids.Any(x => x is null)) throw new BadRequestException("Assignee IDs must not be null");
        var values = ids.Select(x => x!.Value).ToList(); if (values.Distinct().Count() != values.Count) throw new BadRequestException("Duplicate assignee IDs are not allowed");
        var members = await db.StudentGroupMembers.Include(x => x.Student).Where(x => x.GroupId == groupId && values.Contains(x.StudentId)).Select(x => x.Student).ToListAsync(ct);
        if (members.Count != values.Count) throw new BadRequestException("Assignees must be current group members"); return values.Select(id => members.Single(x => x.Id == id)).ToList();
    }

    private async Task<(GroupTask Task, Student Caller)> ChecklistMutation(long groupId, long taskId, string email, CancellationToken ct)
    {
        var group = await LockedGroup(groupId, ct); Writable(group);
        var task = await db.GroupTasks.Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        var caller = await Student(email, "Student profile not found", ct);
        if (!await db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == caller.Id, ct)) throw new ForbiddenException("You are not a member of this student group");
        Mutable(task); var assignee = await db.TaskAssignees.AnyAsync(x => x.TaskId == taskId && x.StudentId == caller.Id, ct);
        if (group.LeaderStudentId != caller.Id && !assignee) throw new ForbiddenException("Only group leader or task assignees can modify checklist items");
        return (task, caller);
    }

    private async Task<(GroupTask Task, Account Caller)> CommentAccess(long groupId, long taskId, string email, bool write, CancellationToken ct)
    {
        var account = await db.Accounts.Include(x => x.Student).Include(x => x.Mentor).Include(x => x.Instructor).FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct) ?? throw new UnauthorizedException("User account not found");
        var task = await db.GroupTasks.Include(x => x.Group).ThenInclude(x => x.TermNavigation).FirstOrDefaultAsync(x => x.Id == taskId && x.GroupId == groupId, ct) ?? throw new NotFoundException("Task not found");
        if (account.Role == "STUDENT")
        {
            if (account.Student is null) throw new ForbiddenException("Student profile not found");
            if (!await db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == account.Student.Id, ct)) throw new ForbiddenException("You are not a member of this student group");
            if (write) Writable(await LockedGroup(groupId, ct));
        }
        else if (account.Role == "MENTOR")
        {
            if (account.Mentor is null) throw new ForbiddenException("Mentor profile not found");
            if (task.Group.MentorId != account.Mentor.Id) throw new ForbiddenException("You are not the assigned mentor for this group");
        }
        else throw new ForbiddenException("Access denied");
        return (task, account);
    }

    private IQueryable<TaskComment> CommentQuery() => db.TaskComments.AsNoTracking().Include(x => x.AuthorAccount).ThenInclude(x => x.Student)
        .Include(x => x.AuthorAccount).ThenInclude(x => x.Mentor).Include(x => x.AuthorAccount).ThenInclude(x => x.Instructor);
    private IQueryable<TaskActivity> ActivityQuery() => db.TaskActivities.AsNoTracking().Include(x => x.ActorAccount).ThenInclude(x => x.Student)
        .Include(x => x.ActorAccount).ThenInclude(x => x.Mentor).Include(x => x.ActorAccount).ThenInclude(x => x.Instructor);

    private async Task<Account> Account(string email, CancellationToken ct) => await db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct) ?? throw new UnauthorizedException("User account not found");
    private async Task<Student> Student(string email, string error, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct) ?? throw new UnauthorizedException("User account not found");
        return await db.Students.Include(x => x.Account).FirstOrDefaultAsync(x => x.AccountId == account.Id, ct) ?? throw new ForbiddenException(error);
    }

    private static void Writable(StudentGroup group)
    {
        Flowzy.Service.Groups.StudentTermWriteGuard.RequireWritable(group);
    }
    private static void Mutable(GroupTask task) { if (task.ArchivedAt is not null) throw new BadRequestException("Cannot modify an archived task"); }
    private static void Version(GroupTask task, long? version) { if (task.Version != version) throw new ConflictException(Stale); }
    private static void Future(DateTime? value) { if (value is not null && Utc(value) <= Now) throw new BadRequestException("Due date must be in the future"); }
    private static DateTime? Utc(DateTime? value) => value is null ? null : value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
    private static string Required(string? value, string message) => string.IsNullOrWhiteSpace(value) ? throw new BadRequestException(message) : value.Trim();
    private static string? ParsePriority(string? value, string message, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return optional ? null : throw new BadRequestException(message); var result = value.Trim();
        return Priorities.Contains(result) ? result : throw new BadRequestException(message);
    }
    private static string ParseStatus(string? value, string blankMessage, string invalidMessage)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new BadRequestException(blankMessage); var result = value.Trim();
        return Statuses.Contains(result) ? result : throw new BadRequestException(invalidMessage);
    }
    private static void Page(int page, int size) { if (page < 0) throw new BadRequestException("Page must be zero or greater"); if (size <= 0) throw new BadRequestException("Size must be greater than zero"); }
    private static object Change(object? oldValue, object? newValue) => new Dictionary<string, object?> { ["old"] = oldValue, ["new"] = newValue };

    private static TaskSummaryResponse Summary(GroupTask task, DateTime now)
    {
        var assignees = task.TaskAssignees.OrderBy(x => x.Student.StudentCode).Select(x => new TaskAssigneeResponse(x.StudentId,
            x.Student.StudentCode, x.Student.FullName, x.Student.Account.Email)).ToList();
        var count = task.TaskChecklistItems.Count; var completed = task.TaskChecklistItems.Count(x => x.Completed);
        return new(task.Id, task.Title, task.Status, task.Priority, task.DueAt, task.Position, task.Version,
            task.CreatedByStudentId, task.CreatedByStudent?.FullName, task.ArchivedAt, task.CreatedAt, task.UpdatedAt,
            assignees, count, completed, count == 0 ? 0 : completed * 100 / count,
            task.ArchivedAt is null && task.Status != "DONE" && task.DueAt is not null && task.DueAt < now);
    }

    private static TaskDetailResponse Detail(GroupTask task, DateTime now)
    {
        var s = Summary(task, now); return new(s.Id, s.Title, task.Description, s.Status, s.Priority, s.DueAt, s.Position,
            s.Version, s.CreatedByStudentId, s.CreatedByStudentName, s.ArchivedAt, s.CreatedAt, s.UpdatedAt, s.Assignees,
            s.ChecklistCount, s.ChecklistCompletedCount, s.ChecklistProgressPercent, s.Overdue,
            task.TaskChecklistItems.OrderBy(x => x.Position).ThenBy(x => x.Id).Select(Checklist).ToList());
    }
    private static ChecklistItemResponse Checklist(TaskChecklistItem x) => new(x.Id, x.Title, x.Completed, x.CompletedByAccountId, x.CompletedAt, x.Position);
    private static TaskCommentResponse Comment(TaskComment x) => new(x.Id, x.AuthorAccountId, x.AuthorAccount.Email,
        Name(x.AuthorAccount), x.AuthorAccount.Role, x.Content, x.EditedAt, x.CreatedAt, x.UpdatedAt);
    private static TaskActivityResponse ActivityResponse(TaskActivity x)
    {
        ActivityActorResponse? actor = x.ActorAccount is null ? null : new(x.ActorAccount.Id, x.ActorAccount.Email, Name(x.ActorAccount), x.ActorAccount.Role);
        IReadOnlyDictionary<string, object?> details;
        try { details = JsonSerializer.Deserialize<Dictionary<string, object?>>(x.Details) ?? new Dictionary<string, object?>(); }
        catch (JsonException) { details = new Dictionary<string, object?>(); }
        return new(x.Id, x.TaskId, x.GroupId, actor, x.ActivityType, details, x.CreatedAt);
    }
    private static string Name(Account x) => x.Role switch { "STUDENT" => x.Student?.FullName ?? "Student", "MENTOR" => x.Mentor?.FullName ?? "Mentor", _ => "Admin" };

    private void Activity(GroupTask task, long? actorId, string type, object details) => db.TaskActivities.Add(new TaskActivity
    {
        TaskId = task.Id, GroupId = task.GroupId, ActorAccountId = actorId, ActivityType = type,
        Details = JsonSerializer.Serialize(details), CreatedAt = Now
    });

    private async Task Notify(IEnumerable<long> recipients, long? actorId, string type, string title, string body, GroupTask task,
        string eventKey, CancellationToken ct, long? commentId = null, Dictionary<string, string>? extraParams = null)
    {
        var now = Now; var values = new Dictionary<string, string> { ["groupId"] = task.GroupId.ToString(), ["taskId"] = task.Id.ToString() };
        if (commentId is not null) values["commentId"] = commentId.Value.ToString();
        if (extraParams is not null) foreach (var pair in extraParams) values[pair.Key] = pair.Value;
        var json = JsonSerializer.Serialize(values);
        var ids = recipients.Distinct().Where(x => x != actorId).ToArray();
        var active = await db.Accounts.Where(x => ids.Contains(x.Id) && x.Status == "ACTIVE").Select(x => x.Id).ToListAsync(ct);
        var notified = await db.Notifications.Where(x => active.Contains(x.RecipientId) && x.EventKey == eventKey).Select(x => x.RecipientId).ToListAsync(ct);
        foreach (var id in active.Except(notified)) db.Notifications.Add(new Notification
        {
            RecipientId = id, Type = type, Title = title, Body = body, ActionKey = "OPEN_TASK", ActionParams = json,
            EntityType = "GroupTask", EntityId = task.Id.ToString(), EventKey = eventKey,
            CreatedAt = now, UpdatedAt = now
        });
    }

    private async Task<IReadOnlyList<long>> LeaderAndAssigneeAccounts(long taskId, long? leaderStudentId, CancellationToken ct)
    {
        var ids = await db.TaskAssignees.Where(x => x.TaskId == taskId).Select(x => x.Student.AccountId).ToListAsync(ct);
        if (leaderStudentId is not null) ids.Add(await db.Students.Where(x => x.Id == leaderStudentId).Select(x => x.AccountId).FirstAsync(ct));
        return ids.Distinct().ToList();
    }

    private static void Touch(GroupTask task) { task.Version++; task.UpdatedAt = Now; }
    private static void Normalize(IReadOnlyList<GroupTask> tasks)
    {
        for (var i = 0; i < tasks.Count; i++) if (tasks[i].Position != i) { tasks[i].Position = i; Touch(tasks[i]); }
    }
}
