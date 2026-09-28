using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class AdminUserRepository(FlowzyDbContext db) : IAdminUserRepository
{
    private IQueryable<Account> WithProfiles() => db.Accounts.Include(x => x.Student!).ThenInclude(x => x.StudentGroupMembers)
        .ThenInclude(x => x.Group).Include(x => x.Mentor).Include(x => x.Instructor);

    public async Task<(List<Account> Items, long Total)> SearchAsync(int page, int size, string? search, string? role, string? status, CancellationToken ct)
    {
        var query = WithProfiles().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.Role == role.ToUpper());
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.ToUpper());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var p = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Email, p)
                || (x.Student != null && (EF.Functions.ILike(x.Student.FullName, p) || EF.Functions.ILike(x.Student.StudentCode, p)))
                || (x.Mentor != null && (EF.Functions.ILike(x.Mentor.FullName, p) || EF.Functions.ILike(x.Mentor.MentorCode, p)))
                || (x.Instructor != null && (EF.Functions.ILike(x.Instructor.FullName, p) || EF.Functions.ILike(x.Instructor.InstructorCode, p))));
        }
        var total = await query.LongCountAsync(ct);
        return (await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip(page * size).Take(size).ToListAsync(ct), total);
    }

    public Task<Account?> FindAsync(long id, CancellationToken ct) => WithProfiles().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Account?> FindByEmailAsync(string email, CancellationToken ct) => WithProfiles().SingleOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);

    public Task<bool> ProfileCodeExistsAsync(string role, string code, long? exceptAccountId, CancellationToken ct) => role switch
    {
        "STUDENT" => db.Students.AnyAsync(x => x.StudentCode == code && (!exceptAccountId.HasValue || x.AccountId != exceptAccountId), ct),
        "MENTOR" => db.Mentors.AnyAsync(x => x.MentorCode == code && (!exceptAccountId.HasValue || x.AccountId != exceptAccountId), ct),
        "INSTRUCTOR" => db.Instructors.AnyAsync(x => x.InstructorCode == code && (!exceptAccountId.HasValue || x.AccountId != exceptAccountId), ct),
        _ => Task.FromResult(false)
    };

    public async Task AddAsync(Account account, CancellationToken ct) { await db.Accounts.AddAsync(account, ct); await db.SaveChangesAsync(ct); }
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
