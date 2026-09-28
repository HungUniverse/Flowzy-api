using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed class TaskBoardRepository(FlowzyDbContext db) : ITaskBoardRepository
{
    public Task<IDbContextTransaction> BeginTransaction(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public Task<Account?> AccountByEmail(string email, CancellationToken ct) => db.Accounts.Include(x => x.Student).Include(x => x.Mentor)
        .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public async Task<StudentGroup?> GroupForUpdate(long groupId, CancellationToken ct)
    {
        var group = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={groupId} FOR UPDATE").FirstOrDefaultAsync(ct);
        if (group is not null) await db.Entry(group).Reference(x => x.TermNavigation).LoadAsync(ct);
        return group;
    }
    public Task<bool> IsMember(long groupId, long studentId, CancellationToken ct) => db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == studentId, ct);
    public Task<List<TaskBoard>> ActiveBoards(long groupId, CancellationToken ct) => db.TaskBoards.Where(x => x.GroupId == groupId && x.ArchivedAt == null).OrderBy(x => x.Position).ToListAsync(ct);
    public Task<List<TaskBoard>> DefaultBoards(long groupId, CancellationToken ct) => db.TaskBoards.Where(x => x.GroupId == groupId && x.DefaultBoard).OrderBy(x => x.Id).ToListAsync(ct);
    public Task<TaskBoard?> Board(long groupId, long boardId, CancellationToken ct) => db.TaskBoards.FirstOrDefaultAsync(x => x.GroupId == groupId && x.Id == boardId, ct);
    public Task<long?> MaxPosition(long groupId, CancellationToken ct) => db.TaskBoards.Where(x => x.GroupId == groupId).MaxAsync(x => (long?)x.Position, ct);
    public Task<bool> HasTasks(long boardId, CancellationToken ct) => db.GroupTasks.AnyAsync(x => x.BoardId == boardId, ct);
    public void Add(TaskBoard board) => db.TaskBoards.Add(board);
    public void Remove(TaskBoard board) => db.TaskBoards.Remove(board);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
