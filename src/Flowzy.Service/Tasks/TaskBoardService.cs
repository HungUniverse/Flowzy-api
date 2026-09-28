using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Tasks;

public sealed class TaskBoardService(ITaskBoardRepository repository) : ITaskBoardService
{
    public async Task<IReadOnlyList<TaskBoardResponse>> GetBoardsAsync(long groupId, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginTransaction(ct);
        var caller = await Account(email, ct); var group = await Group(groupId, ct);
        if (caller.Role == "STUDENT")
        {
            var student = caller.Student ?? throw new ForbiddenException("Student profile not found");
            if (!await repository.IsMember(groupId, student.Id, ct)) throw new ForbiddenException("You are not a member of this student group");
        }
        else if (caller.Role == "MENTOR")
        {
            var mentor = caller.Mentor ?? throw new ForbiddenException("Mentor profile not found");
            if (group.MentorId != mentor.Id) throw new ForbiddenException("You are not the assigned mentor for this group");
        }
        else throw new ForbiddenException("Access denied");
        var boards = await repository.ActiveBoards(groupId, ct);
        if (boards.Count == 0 && (await repository.DefaultBoards(groupId, ct)).Count == 0)
        {
            var now = DateTime.UtcNow;
            var board = new TaskBoard { GroupId = groupId, Name = "Default", Description = "Default Board", Position = 0,
                DefaultBoard = true, CreatedAt = now, UpdatedAt = now };
            repository.Add(board); await repository.Save(ct); boards.Add(board);
        }
        await transaction.CommitAsync(ct); return boards.Select(Map).ToList();
    }

    public async Task<TaskBoardResponse> CreateBoardAsync(long groupId, CreateTaskBoardRequest request, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginTransaction(ct);
        var student = await Leader(groupId, email, "create", ct);
        var now = DateTime.UtcNow;
        var board = new TaskBoard { GroupId = groupId, Name = request.Name.Trim(), Description = request.Description,
            Position = (await repository.MaxPosition(groupId, ct) ?? -1) + 1, DefaultBoard = false,
            CreatedByStudentId = student.Id, CreatedAt = now, UpdatedAt = now };
        repository.Add(board); await repository.Save(ct); await transaction.CommitAsync(ct); return Map(board);
    }

    public async Task<TaskBoardResponse> UpdateBoardAsync(long groupId, long boardId, UpdateTaskBoardRequest request, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginTransaction(ct);
        await Leader(groupId, email, "update", ct);
        var board = await repository.Board(groupId, boardId, ct) ?? throw new NotFoundException("Task board not found");
        if ((request.Archived ?? board.ArchivedAt is not null) && (request.DefaultBoard ?? board.DefaultBoard))
            throw new BadRequestException("Cannot archive the default board");
        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length == 0) throw new BadRequestException("Board name cannot be blank");
            if (name.Length > 255) throw new BadRequestException("Board name must be at most 255 characters");
            board.Name = name;
        }
        if (request.Description is not null) board.Description = request.Description;
        if (request.Position is not null) board.Position = request.Position.Value;
        if (request.Archived is not null) board.ArchivedAt = request.Archived.Value ? DateTime.UtcNow : null;
        if (request.DefaultBoard == true && !board.DefaultBoard)
        {
            foreach (var current in await repository.DefaultBoards(groupId, ct)) current.DefaultBoard = false;
            // Flush demotions first: PostgreSQL checks the partial unique index per statement.
            await repository.Save(ct); board.DefaultBoard = true;
        }
        else if (request.DefaultBoard == false && board.DefaultBoard)
            throw new BadRequestException("Cannot unset default board directly; set another board as default instead");
        board.UpdatedAt = DateTime.UtcNow; await repository.Save(ct); await transaction.CommitAsync(ct); return Map(board);
    }

    public async Task DeleteBoardAsync(long groupId, long boardId, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginTransaction(ct);
        await Leader(groupId, email, "delete", ct);
        var board = await repository.Board(groupId, boardId, ct) ?? throw new NotFoundException("Task board not found");
        if (board.DefaultBoard) throw new BadRequestException("Cannot delete the default board");
        if (await repository.HasTasks(boardId, ct)) throw new ConflictException("Cannot delete a task board that contains tasks. Archive it or move its tasks to another board first.");
        repository.Remove(board); await repository.Save(ct); await transaction.CommitAsync(ct);
    }

    private async Task<Student> Leader(long groupId, string email, string action, CancellationToken ct)
    {
        var caller = await Account(email, ct); var group = await Group(groupId, ct);
        if (caller.Role != "STUDENT") throw new ForbiddenException($"Only students can {action} task boards");
        Flowzy.Service.Groups.StudentTermWriteGuard.RequireWritable(group);
        var student = caller.Student ?? throw new ForbiddenException("Student profile not found");
        if (group.LeaderStudentId != student.Id) throw new ForbiddenException($"Only the group leader is authorized to {action} task boards");
        return student;
    }
    private async Task<Account> Account(string email, CancellationToken ct) => await repository.AccountByEmail(email, ct) ?? throw new UnauthorizedException("User account not found");
    private async Task<StudentGroup> Group(long id, CancellationToken ct) => await repository.GroupForUpdate(id, ct) ?? throw new NotFoundException("Group not found");
    private static TaskBoardResponse Map(TaskBoard board) => new(board.Id, board.GroupId, board.Name, board.Description, board.Position,
        board.DefaultBoard, board.CreatedByStudentId, board.ArchivedAt, board.CreatedAt, board.UpdatedAt);
}
