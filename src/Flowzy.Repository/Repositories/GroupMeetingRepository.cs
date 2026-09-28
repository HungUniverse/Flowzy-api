using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace Flowzy.Repository.Repositories;
public sealed class GroupMeetingRepository(FlowzyDbContext db) : IGroupMeetingRepository
{
    public Task<IDbContextTransaction> Begin(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    private IQueryable<StudentGroup> Groups => db.StudentGroups.Include(x => x.TermNavigation).Include(x => x.SelectedProblem)
        .Include(x => x.LeaderStudent).Include(x => x.Mentor).Include(x => x.Instructor);
    private IQueryable<MentorMeeting> Meetings => db.MentorMeetings.Include(x => x.Group).ThenInclude(x => x.SelectedProblem)
        .Include(x => x.Group).ThenInclude(x => x.Mentor).Include(x => x.Group).ThenInclude(x => x.Instructor)
        .Include(x => x.Group).ThenInclude(x => x.LeaderStudent).Include(x => x.Mentor).Include(x => x.Slot)
        .Include(x => x.BookedByStudent).Include(x => x.EvidenceSubmittedByStudent);
    public async Task<StudentGroup?> Group(long id, bool locked, CancellationToken ct)
    {
        if (locked)
        {
            var row = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={id} FOR UPDATE").FirstOrDefaultAsync(ct);
            if (row is null) return null;
            await db.Entry(row).ReloadAsync(ct);
        }
        return await Groups.FirstOrDefaultAsync(x => x.Id == id, ct);
    }
    public Task<MentorMeeting?> Meeting(long id, CancellationToken ct) => Meetings.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task Reload(MentorMeeting meeting, CancellationToken ct) => db.Entry(meeting).ReloadAsync(ct);
    public Task<Account?> Account(string email, CancellationToken ct) => db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public Task<Student?> Student(string email, CancellationToken ct) => db.Students.Include(x => x.Account).FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public async Task<Mentor?> Mentor(string email, bool locked, CancellationToken ct)
    {
        if (locked) return await db.Mentors.FromSqlInterpolated($"SELECT m.* FROM mentors m JOIN accounts a ON a.id=m.account_id WHERE a.email={email} FOR UPDATE OF m").FirstOrDefaultAsync(ct);
        return await db.Mentors.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    }
    public Task<Instructor?> Instructor(string email, CancellationToken ct) => db.Instructors.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<bool> IsMember(long group, long student, CancellationToken ct) => db.StudentGroupMembers.AnyAsync(x => x.GroupId == group && x.StudentId == student, ct);
    public Task<List<long>> MemberAccounts(long group, CancellationToken ct) => db.StudentGroupMembers.Where(x => x.GroupId == group).Select(x => x.Student.AccountId).ToListAsync(ct);
    public Task<List<MentorAvailabilitySlot>> Availability(long mentor, DateTime now, CancellationToken ct) => db.MentorAvailabilitySlots.Include(x => x.Mentor)
        .Where(x => x.MentorId == mentor && x.Status == "AVAILABLE" && x.StartAt > now).OrderBy(x => x.StartAt).ToListAsync(ct);
    public Task<MentorAvailabilitySlot?> Slot(long id, CancellationToken ct) => db.MentorAvailabilitySlots
        .FromSqlInterpolated($"SELECT * FROM mentor_availability_slots WHERE id={id} FOR UPDATE").FirstOrDefaultAsync(ct);
    public Task<long> ActiveCount(long group, CancellationToken ct) => db.MentorMeetings.LongCountAsync(x => x.GroupId == group && (x.Status == "SCHEDULED" || x.Status == "COMPLETED"), ct);
    public Task<bool> Overlap(long mentor, DateTime start, DateTime end, long? exclude, CancellationToken ct) => db.MentorMeetings
        .AnyAsync(x => x.MentorId == mentor && x.Status != "CANCELED" && (!exclude.HasValue || x.Id != exclude) && x.StartAt < end && x.EndAt > start, ct);
    public Task<List<MentorMeeting>> List(long group, CancellationToken ct) => Meetings.Where(x => x.GroupId == group).OrderByDescending(x => x.StartAt).ToListAsync(ct);
    public async Task Notify(Notification notification, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x => x.Id == notification.RecipientId && x.Status == "ACTIVE", ct)) return;
        if (db.Notifications.Local.Any(x => x.RecipientId == notification.RecipientId && x.EventKey == notification.EventKey) ||
            await db.Notifications.AnyAsync(x => x.RecipientId == notification.RecipientId && x.EventKey == notification.EventKey, ct)) return;
        db.Notifications.Add(notification);
    }
    public void Add(MentorMeeting meeting) => db.MentorMeetings.Add(meeting);
    public Task Save(CancellationToken ct) => db.SaveChangesAsync(ct);
}
