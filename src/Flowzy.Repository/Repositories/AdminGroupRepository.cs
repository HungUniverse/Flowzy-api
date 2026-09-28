using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class AdminGroupRepository(FlowzyDbContext db) : IAdminGroupRepository
{
    public async Task<(List<StudentGroup> Items, long Total)> SearchAsync(int page, int size, string? search,
        string? status, CancellationToken cancellationToken = default)
    {
        var query = db.StudentGroups.AsNoTracking();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern)
                || (x.ProjectName != null && EF.Functions.ILike(x.ProjectName, pattern))
                || EF.Functions.ILike(x.CourseCode, pattern)
                || EF.Functions.ILike(x.Term, pattern));
        }
        var total = await query.LongCountAsync(cancellationToken);
        var ids = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(page * size).Take(size).Select(x => x.Id).ToListAsync(cancellationToken);
        if (ids.Count == 0) return ([], total);
        var details = await db.StudentGroups.AsNoTracking().AsSplitQuery()
            .Include(x => x.TermNavigation)
            .Include(x => x.LeaderStudent)
            .Include(x => x.StudentGroupMembers)
            .Include(x => x.Mentor).ThenInclude(x => x!.Account)
            .Include(x => x.Instructor).ThenInclude(x => x!.Account)
            .Include(x => x.SelectedProblem)
            .Include(x => x.GroupRecruitmentNeeds)
            .Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        var byId = details.ToDictionary(x => x.Id);
        return (ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList(), total);
    }
}
