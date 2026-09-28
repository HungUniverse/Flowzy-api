using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class PlatformReadRepository(FlowzyDbContext db) : IPlatformReadRepository
{
    public Task<Account?> FindProfileAsync(string email, CancellationToken ct) => db.Accounts
        .Include(x => x.Student!).ThenInclude(x => x.StudentGroupMembers).ThenInclude(x => x.Group)
        .Include(x => x.Mentor).Include(x => x.Instructor)
        .SingleOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<List<AcademicTerm>> GetOpenTermsAsync(CancellationToken ct) => db.AcademicTerms
        .Include(x => x.ClosedByAccount).Where(x => x.Status == "OPEN")
        .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);

    public async Task<(long Groups, long ExpectedFeedback, long SubmittedFeedback)> GetTermCountsAsync(long termId, string termCode, CancellationToken ct)
    {
        var groups = await db.StudentGroups.LongCountAsync(x => x.Term.ToLower() == termCode.ToLower(), ct);
        var expected = await db.TermFeedbacks.LongCountAsync(x => x.AcademicTermId == termId, ct);
        var submitted = await db.TermFeedbacks.LongCountAsync(x => x.AcademicTermId == termId && x.Status == "SUBMITTED", ct);
        return (groups, expected, submitted);
    }

    public Task<List<ProblemDomain>> GetProblemDomainsAsync(string? search, string? status, CancellationToken ct)
    {
        var query = db.ProblemDomains.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.ToUpper());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, pattern) || EF.Functions.ILike(x.Name, pattern)
                || (x.Description != null && EF.Functions.ILike(x.Description, pattern)));
        }
        return query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
    }

    public Task<List<ProblemEvaluationCriterion>> GetActiveCriteriaAsync(CancellationToken ct) => db.ProblemEvaluationCriteria
        .AsNoTracking().Where(x => x.Active).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);

    public async Task<(List<Problem> Items, long Total)> SearchProblemsAsync(int page, int size, string? search, string? domainCode,
        string? difficulty, string? expectedOutput, string? sourceType, string? status, CancellationToken ct)
    {
        var query = db.Problems.AsNoTracking().Include(x => x.Domain).Include(x => x.ProposedByGroup).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => (x.Code != null && EF.Functions.ILike(x.Code, pattern)) || EF.Functions.ILike(x.Title, pattern)
                || EF.Functions.ILike(x.Statement, pattern));
        }
        if (!string.IsNullOrWhiteSpace(domainCode)) query = query.Where(x => x.Domain != null && x.Domain.Code.ToLower() == domainCode.ToLower());
        if (!string.IsNullOrWhiteSpace(difficulty)) query = query.Where(x => x.DifficultyLevel == difficulty.ToUpper());
        if (!string.IsNullOrWhiteSpace(expectedOutput)) query = query.Where(x => x.ExpectedOutput != null && EF.Functions.ILike(x.ExpectedOutput, $"%{expectedOutput}%"));
        if (!string.IsNullOrWhiteSpace(sourceType)) query = query.Where(x => x.SourceType == sourceType.ToUpper());
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.ToUpper());
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip(page * size).Take(size).ToListAsync(ct);
        return (items, total);
    }

    public Task<Problem?> FindProblemAsync(long id, CancellationToken ct) => db.Problems.AsNoTracking()
        .Include(x => x.Domain).Include(x => x.ProposedByGroup).Include(x => x.ProposedByStudent)
        .Include(x => x.ReviewedByAccount).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<(List<Notification> Items, long Total)> GetNotificationsAsync(long recipientId, bool unreadOnly, int page, int size, CancellationToken ct)
    {
        var query = db.Notifications.Where(x => x.RecipientId == recipientId);
        if (unreadOnly) query = query.Where(x => x.ReadAt == null);
        var total = await query.LongCountAsync(ct);
        var items = await query.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(page * size).Take(size).ToListAsync(ct);
        return (items, total);
    }

    public Task<long> GetUnreadCountAsync(long recipientId, CancellationToken ct) =>
        db.Notifications.LongCountAsync(x => x.RecipientId == recipientId && x.ReadAt == null, ct);

    public Task<Notification?> FindNotificationAsync(long id, CancellationToken ct) => db.Notifications.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task MarkAllNotificationsReadAsync(long recipientId, DateTime readAt, CancellationToken ct)
    {
        await db.Notifications.Where(x => x.RecipientId == recipientId && x.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, readAt).SetProperty(x => x.UpdatedAt, readAt), ct);
    }
}
