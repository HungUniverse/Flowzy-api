using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class MilestoneGradeRepository(FlowzyDbContext db) : IMilestoneGradeRepository
{
    public Task<MilestoneSubmission?> FindSubmissionAsync(long submissionId, CancellationToken cancellationToken = default) =>
        db.MilestoneSubmissions
            .AsNoTracking()
            .Include(x => x.Milestone)
            .Include(x => x.Group).ThenInclude(x => x.StudentGroupMembers).ThenInclude(x => x.Student).ThenInclude(x => x.Account)
            .Include(x => x.Group).ThenInclude(x => x.LeaderStudent)
            .Include(x => x.Group).ThenInclude(x => x.Instructor)
            .SingleOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

    public Task<MilestoneGrade?> FindBySubmissionIdAsync(long submissionId, CancellationToken cancellationToken = default) =>
        db.MilestoneGrades
            .AsNoTracking()
            .Include(x => x.Submission).ThenInclude(x => x.Milestone)
            .Include(x => x.Instructor)
            .SingleOrDefaultAsync(x => x.SubmissionId == submissionId, cancellationToken);

    public Task<StudentGroup?> FindGroupAsync(long groupId, CancellationToken cancellationToken = default) =>
        db.StudentGroups
            .AsNoTracking()
            .Include(x => x.StudentGroupMembers).ThenInclude(x => x.Student)
            .Include(x => x.LeaderStudent)
            .Include(x => x.Instructor)
            .SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);

    public Task<List<MilestoneGrade>> FindByGroupIdAsync(long groupId, CancellationToken cancellationToken = default) =>
        db.MilestoneGrades
            .AsNoTracking()
            .Include(x => x.Submission).ThenInclude(x => x.Milestone)
            .Include(x => x.Instructor)
            .Where(x => x.Submission.GroupId == groupId)
            .ToListAsync(cancellationToken);
}
