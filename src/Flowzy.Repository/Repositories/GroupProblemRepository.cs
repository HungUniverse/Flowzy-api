using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed class GroupProblemRepository(FlowzyDbContext db) : IGroupProblemRepository
{
    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public async Task<StudentGroup?> GroupAsync(long id, bool forUpdate, CancellationToken ct)
    {
        var group = forUpdate
            ? await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={id} FOR UPDATE").FirstOrDefaultAsync(ct)
            : await db.StudentGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (group is not null) await db.Entry(group).Reference(x => x.TermNavigation).LoadAsync(ct);
        return group;
    }
    public Task<Student?> StudentAsync(string email, CancellationToken ct) => db.Students.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<bool> IsMemberAsync(long group, long student, CancellationToken ct) => db.StudentGroupMembers.AnyAsync(x => x.GroupId == group && x.StudentId == student, ct);
    public Task<Problem?> ProblemAsync(long id, CancellationToken ct) => db.Problems.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<ProblemDomain?> DomainAsync(string code, CancellationToken ct) => db.ProblemDomains.FirstOrDefaultAsync(x => x.Code == code, ct);
    public Task<List<Problem>> ProposalsAsync(long group, CancellationToken ct) => db.Problems.AsNoTracking()
        .Include(x => x.Domain).Include(x => x.ProposedByGroup).Where(x => x.ProposedByGroupId == group).ToListAsync(ct);
    public void Add(Problem problem) => db.Problems.Add(problem);
    public void Remove(Problem problem) => db.Problems.Remove(problem);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
