using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed class AcademicTermRepository(FlowzyDbContext db) : IAcademicTermRepository
{
    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public async Task<(List<AcademicTerm> Items, long Total)> PageAsync(int page, int size, CancellationToken ct) =>
        (await db.AcademicTerms.AsNoTracking().Include(x => x.ClosedByAccount).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(page * size).Take(size).ToListAsync(ct), await db.AcademicTerms.LongCountAsync(ct));
    public async Task<AcademicTerm?> FindAsync(string code, bool locked, CancellationToken ct)
    {
        var term = locked
            ? await db.AcademicTerms.FromSqlInterpolated($"SELECT * FROM academic_terms WHERE UPPER(code)={code} FOR UPDATE").FirstOrDefaultAsync(ct)
            : await db.AcademicTerms.FirstOrDefaultAsync(x => x.Code.ToUpper() == code, ct);
        if (term is not null)
        {
            // A pre-lock lookup must not mask a concurrent close that just committed.
            if (locked) await db.Entry(term).ReloadAsync(ct);
            await db.Entry(term).Reference(x => x.ClosedByAccount).LoadAsync(ct);
        }
        return term;
    }
    public Task<AcademicTerm?> LatestOpenAsync(CancellationToken ct) => db.AcademicTerms.Where(x => x.Status == "OPEN")
        .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
    public Task<Account?> AccountAsync(string email, CancellationToken ct) => db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public Task<bool> HasGroupsAsync(string code, CancellationToken ct) => db.StudentGroups.AnyAsync(x => x.Term.ToUpper() == code, ct);
    public async Task<(long Groups, long Expected, long Submitted)> CountsAsync(AcademicTerm term, CancellationToken ct) =>
        (await db.StudentGroups.LongCountAsync(x => x.Term.ToUpper() == term.Code.ToUpper(), ct),
         await db.TermFeedbacks.LongCountAsync(x => x.AcademicTermId == term.Id, ct),
         await db.TermFeedbacks.LongCountAsync(x => x.AcademicTermId == term.Id && x.Status == "SUBMITTED", ct));
    public async Task<List<StudentGroup>> LockGroupsAsync(string code, CancellationToken ct)
    {
        var groups = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE UPPER(term)={code} ORDER BY id FOR UPDATE").ToListAsync(ct);
        var ids = groups.Select(x => x.Id).ToArray();
        if (ids.Length == 0) return groups;
        return await db.StudentGroups.Where(x => ids.Contains(x.Id)).Include(x => x.StudentGroupMembers)
            .ThenInclude(x => x.Student).ThenInclude(x => x.Account).AsSplitQuery().OrderBy(x => x.Id).ToListAsync(ct);
    }
    public async Task CancelPendingAsync(string code, DateTime now, CancellationToken ct)
    {
        await db.GroupInvitations.Where(x => x.Group.Term.ToUpper() == code && x.Status == "PENDING")
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, "CANCELED").SetProperty(x => x.RespondedAt, now), ct);
        await db.GroupJoinRequests.Where(x => x.Group.Term.ToUpper() == code && x.Status == "PENDING")
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, "CANCELED").SetProperty(x => x.RespondedAt, now), ct);
    }
    public Task<bool> HasFeedbackAsync(long term, long group, long student, string target, CancellationToken ct) => db.TermFeedbacks
        .AnyAsync(x => x.AcademicTermId == term && x.GroupId == group && x.StudentId == student && x.TargetType == target, ct);
    public Task<List<Student>> StudentsAsync(string code, CancellationToken ct) => db.Students.Include(x => x.Account)
        .Where(x => x.StudentGroupMembers.Any(m => m.Group.Term.ToUpper() == code)).ToListAsync(ct);
    public async Task<HashSet<long>> StudentsInOtherOpenTermsAsync(string code, CancellationToken ct)
    {
        var open = db.AcademicTerms.Where(x => x.Status == "OPEN").Select(x => x.Code.ToUpper());
        return (await db.StudentGroupMembers.Where(x => x.Group.Term.ToUpper() != code && open.Contains(x.Group.Term.ToUpper()))
            .Select(x => x.StudentId).Distinct().ToListAsync(ct)).ToHashSet();
    }
    public async Task AddNotificationAsync(Notification notification, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x => x.Id == notification.RecipientId && x.Status == "ACTIVE", ct)) return;
        if (db.Notifications.Local.Any(x => x.RecipientId == notification.RecipientId && x.EventKey == notification.EventKey) ||
            await db.Notifications.AnyAsync(x => x.RecipientId == notification.RecipientId && x.EventKey == notification.EventKey, ct)) return;
        db.Notifications.Add(notification);
    }
    public void Add(AcademicTerm term) => db.AcademicTerms.Add(term);
    public void Add(TermFeedback feedback) => db.TermFeedbacks.Add(feedback);
    public void Remove(AcademicTerm term) => db.AcademicTerms.Remove(term);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
