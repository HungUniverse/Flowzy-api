using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace Flowzy.Repository.Repositories;
public sealed class GradeMatrixRepository(FlowzyDbContext db) : IGradeMatrixRepository
{
    public Task<IDbContextTransaction> Begin(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public Task<Account?> Account(string email, CancellationToken ct) => db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public Task<Instructor?> Instructor(string email, bool locked, CancellationToken ct) => locked
        ? db.Instructors.FromSqlInterpolated($"SELECT i.* FROM instructors i JOIN accounts a ON a.id=i.account_id WHERE a.email={email} FOR UPDATE OF i").FirstOrDefaultAsync(ct)
        : db.Instructors.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<Student?> Student(string email, CancellationToken ct) => db.Students.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<CourseMilestone?> Milestone(long id, CancellationToken ct) => db.CourseMilestones.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<StudentGroup?> Group(long id, bool locked, CancellationToken ct)
    {
        if (locked && await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={id} FOR UPDATE").FirstOrDefaultAsync(ct) is null) return null;
        return await db.StudentGroups.Include(x => x.TermNavigation).Include(x => x.LeaderStudent).Include(x => x.Instructor).FirstOrDefaultAsync(x => x.Id == id, ct);
    }
    public Task<List<StudentGroup>> AssignedGroups(long instructor, string? term, string? course, CancellationToken ct) => db.StudentGroups
        .Where(x => x.InstructorId == instructor && x.Status == "ACTIVE" && x.StudentGroupMembers.Any() && (term == null || x.Term == term) && (course == null || x.CourseCode == course))
        .OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
    public Task<List<CourseMilestone>> OwnedMilestones(long instructor, CancellationToken ct) => db.CourseMilestones.Where(x => x.InstructorId == instructor).ToListAsync(ct);
    public Task<List<CourseMilestone>> ScopeMilestones(string term, string course, CancellationToken ct) => db.CourseMilestones.Where(x => x.Term.ToLower() == term.ToLower() && x.CourseCode.ToLower() == course.ToLower()).ToListAsync(ct);
    public Task<List<StudentGroupMember>> Members(long group, CancellationToken ct) => db.StudentGroupMembers.Include(x => x.Student).ThenInclude(x => x.Account).Where(x => x.GroupId == group).ToListAsync(ct);
    public Task<List<MilestoneMemberScore>> Scores(long group, CancellationToken ct) => db.MilestoneMemberScores.Include(x => x.Student).Where(x => x.GroupId == group).ToListAsync(ct);
    public Task<List<MilestoneGroupGrade>> Grades(long group, CancellationToken ct) => db.MilestoneGroupGrades.Where(x => x.GroupId == group).ToListAsync(ct);
    public Task<List<MilestoneContributionRevision>> Revisions(long group, CancellationToken ct) => db.MilestoneContributionRevisions.Where(x => x.GroupId == group).ToListAsync(ct);
    public Task<MilestoneContributionRevision?> LockRevision(long milestone, long group, CancellationToken ct) => db.MilestoneContributionRevisions
        .FromSqlInterpolated($"SELECT * FROM milestone_contribution_revisions WHERE milestone_id={milestone} AND group_id={group} FOR UPDATE").FirstOrDefaultAsync(ct);
    public Task<List<MilestoneContributionAgreement>> Agreements(long revision, CancellationToken ct) => db.MilestoneContributionAgreements.Where(x => x.RevisionId == revision).ToListAsync(ct);
    public void ClearAgreements(IEnumerable<MilestoneContributionAgreement> agreements) => db.MilestoneContributionAgreements.RemoveRange(agreements);
    public void Add<T>(T entity) where T : class => db.Add(entity);
    public Task Save(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task Notify(Notification notice, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x => x.Id == notice.RecipientId && x.Status == "ACTIVE", ct)) return;
        if (db.Notifications.Local.Any(x => x.RecipientId == notice.RecipientId && x.EventKey == notice.EventKey) || await db.Notifications.AnyAsync(x => x.RecipientId == notice.RecipientId && x.EventKey == notice.EventKey, ct)) return;
        db.Notifications.Add(notice);
    }
}
