using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace Flowzy.Repository.Repositories;
public sealed class CourseMilestoneRepository(FlowzyDbContext db) : ICourseMilestoneRepository
{
    public Task<IDbContextTransaction> Begin(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public Task<Account?> Account(string email, CancellationToken ct) => db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public Task<Instructor?> Instructor(string email, bool locked, CancellationToken ct) => locked
        ? db.Instructors.FromSqlInterpolated($"SELECT i.* FROM instructors i JOIN accounts a ON a.id=i.account_id WHERE a.email={email} FOR UPDATE OF i").FirstOrDefaultAsync(ct)
        : db.Instructors.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<Student?> Student(string email, CancellationToken ct) => db.Students.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<AcademicTerm?> Term(string code, CancellationToken ct) => db.AcademicTerms.FirstOrDefaultAsync(x => x.Code.ToLower() == code.ToLower(), ct);
    public Task<List<StudentGroup>> AssignedGroups(long instructor, string term, string course, CancellationToken ct) => db.StudentGroups.Include(x => x.LeaderStudent)
        .Where(x => x.InstructorId == instructor && x.Term.ToLower() == term.ToLower() && x.CourseCode.ToLower() == course.ToLower()).ToListAsync(ct);
    public Task<List<StudentGroup>> StudentGroups(long student, CancellationToken ct) => db.StudentGroupMembers.Where(x => x.StudentId == student).Select(x => x.Group).ToListAsync(ct);
    public Task<StudentGroup?> Group(long id, CancellationToken ct) => db.StudentGroups.Include(x => x.StudentGroupMembers).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<CourseMilestone?> Get(long id, CancellationToken ct) => db.CourseMilestones.Include(x => x.Instructor).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<CourseMilestone>> Owned(long instructor, CancellationToken ct) => db.CourseMilestones.Include(x => x.Instructor).Where(x => x.InstructorId == instructor).ToListAsync(ct);
    public Task<List<CourseMilestone>> Scope(long instructor, string term, string course, CancellationToken ct) => db.CourseMilestones.Include(x => x.Instructor)
        .Where(x => x.InstructorId == instructor && x.Term.ToLower() == term.ToLower() && x.CourseCode.ToLower() == course.ToLower()).OrderBy(x => x.Position).ToListAsync(ct);
    public async Task<bool> HasHigherGrade(long id, decimal max, CancellationToken ct) =>
        await db.MilestoneGrades.AnyAsync(x => x.Submission.MilestoneId == id && x.Score > max, ct) || await db.MilestoneGroupGrades.AnyAsync(x => x.MilestoneId == id && x.Score > max, ct);
    public Task<List<MilestoneSubmission>> Submissions(long id, CancellationToken ct) => db.MilestoneSubmissions.Where(x => x.MilestoneId == id).ToListAsync(ct);
    public async Task Notify(Notification notice, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x => x.Id == notice.RecipientId && x.Status == "ACTIVE", ct)) return;
        if (db.Notifications.Local.Any(x => x.RecipientId == notice.RecipientId && x.EventKey == notice.EventKey) ||
            await db.Notifications.AnyAsync(x => x.RecipientId == notice.RecipientId && x.EventKey == notice.EventKey, ct)) return;
        db.Notifications.Add(notice);
    }
    public void Add(CourseMilestone milestone) => db.CourseMilestones.Add(milestone);
    public Task Save(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task Reload(CourseMilestone milestone, CancellationToken ct) => db.Entry(milestone).ReloadAsync(ct);
}
