using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class ProblemReviewRepository(FlowzyDbContext db) : IProblemReviewRepository
{
    public Task<List<Problem>> FindPendingForInstructorAsync(long instructorId, CancellationToken cancellationToken = default) =>
        db.Problems.AsNoTracking()
            .Include(x => x.Domain)
            .Include(x => x.ProposedByGroup)
            .Where(x => x.SourceType == "SELF_PROPOSED"
                && x.Status == "PENDING_REVIEW"
                && x.ProposedByGroup != null
                && x.ProposedByGroup.InstructorId == instructorId)
            .ToListAsync(cancellationToken);

    public Task<Problem?> FindForReviewAsync(long problemId, CancellationToken cancellationToken = default) =>
        db.Problems
            .Include(x => x.Domain)
            .Include(x => x.ProposedByGroup).ThenInclude(x => x.Instructor)
            .Include(x => x.ProposedByStudent)
            .Include(x => x.ReviewedByAccount)
            .SingleOrDefaultAsync(x => x.Id == problemId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
