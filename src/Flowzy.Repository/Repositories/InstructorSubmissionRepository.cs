using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class InstructorSubmissionRepository(FlowzyDbContext db) : IInstructorSubmissionRepository
{
    public Task<List<MilestoneSubmission>> FindAllForInstructorAsync(long instructorId, string? term,
        string? courseCode, long? milestoneId, long? groupId, string? status, bool? late,
        CancellationToken cancellationToken = default)
    {
        var query = db.MilestoneSubmissions.AsNoTracking()
            .Include(x => x.MilestoneGrade)
            .Include(x => x.Milestone)
            .Include(x => x.Group)
            .Where(x => x.Milestone.InstructorId == instructorId && x.Group.InstructorId == instructorId);

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.Milestone.Term.ToLower() == term.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(courseCode))
            query = query.Where(x => x.Milestone.CourseCode.ToLower() == courseCode.Trim().ToLower());
        if (milestoneId.HasValue) query = query.Where(x => x.MilestoneId == milestoneId.Value);
        if (groupId.HasValue) query = query.Where(x => x.GroupId == groupId.Value);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (late.HasValue) query = query.Where(x => x.Late == late.Value);
        return query.ToListAsync(cancellationToken);
    }
}
