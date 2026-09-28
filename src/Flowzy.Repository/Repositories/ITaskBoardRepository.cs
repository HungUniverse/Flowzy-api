using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public interface ITaskBoardRepository
{
    Task<IDbContextTransaction> BeginTransaction(CancellationToken ct);
    Task<Account?> AccountByEmail(string email, CancellationToken ct);
    Task<StudentGroup?> GroupForUpdate(long groupId, CancellationToken ct);
    Task<bool> IsMember(long groupId, long studentId, CancellationToken ct);
    Task<List<TaskBoard>> ActiveBoards(long groupId, CancellationToken ct);
    Task<List<TaskBoard>> DefaultBoards(long groupId, CancellationToken ct);
    Task<TaskBoard?> Board(long groupId, long boardId, CancellationToken ct);
    Task<long?> MaxPosition(long groupId, CancellationToken ct);
    Task<bool> HasTasks(long boardId, CancellationToken ct);
    void Add(TaskBoard board);
    void Remove(TaskBoard board);
    Task Save(CancellationToken ct);
}
