using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class StudentDiscoveryRepository(FlowzyDbContext db) : IStudentDiscoveryRepository
{
    public Task<Student?> FindActiveByIdAsync(long id, CancellationToken cancellationToken = default) =>
        db.Students.AsNoTracking()
            .Include(x => x.Account)
            .SingleOrDefaultAsync(x => x.Id == id
                && x.Status == "ACTIVE"
                && x.Account.Status == "ACTIVE", cancellationToken);

    public async Task<(List<Student> Items, long Total)> FindUngroupedAsync(string term, string courseCode,
        string? search, int page, int size, CancellationToken cancellationToken = default)
    {
        var query = db.Students.AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.Status == "ACTIVE"
                && !db.StudentGroupMembers.Any(member => member.StudentId == x.Id
                    && member.Group.Term == term
                    && member.Group.CourseCode == courseCode));

        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(x => EF.Functions.ILike(x.FullName, pattern)
                || EF.Functions.ILike(x.StudentCode, pattern)
                || EF.Functions.ILike(x.Account.Email, pattern));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(page * size).Take(size).ToListAsync(cancellationToken);
        return (items, total);
    }
}
