using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class MilestoneSubmissionRepository(FlowzyDbContext db) : IMilestoneSubmissionRepository
{
    private IQueryable<MilestoneSubmission> Query => db.MilestoneSubmissions.AsNoTracking()
        .Include(x => x.Milestone).Include(x => x.MilestoneGrade).Include(x => x.Group);
    public Task<MilestoneSubmission?> FindAsync(long id, CancellationToken ct) => Query.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<StudentGroup?> GroupAsync(long id, CancellationToken ct) => db.StudentGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<CourseMilestone?> MilestoneAsync(long id, CancellationToken ct) => db.CourseMilestones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<Student?> StudentAsync(string email, CancellationToken ct) => db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<Instructor?> InstructorAsync(string email, CancellationToken ct) => db.Instructors.AsNoTracking().FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<bool> IsMemberAsync(long groupId, long studentId, CancellationToken ct) => db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == studentId, ct);
    public Task<List<MilestoneSubmission>> ByGroupAsync(long id, CancellationToken ct) => Query.Where(x => x.GroupId == id).ToListAsync(ct);
    public Task<List<MilestoneSubmission>> ByMilestoneAsync(long id, long currentInstructorId, CancellationToken ct) => Query
        .Where(x => x.MilestoneId == id && x.Group.InstructorId == currentInstructorId).ToListAsync(ct);
}
