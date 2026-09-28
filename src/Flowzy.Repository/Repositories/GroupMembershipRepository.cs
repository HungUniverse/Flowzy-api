using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed class GroupMembershipRepository(FlowzyDbContext db) : IGroupMembershipRepository
{
    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public Task<Account?> AccountAsync(string email, CancellationToken ct) => db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public Task<Student?> StudentAsync(string email, CancellationToken ct) => db.Students.Include(x => x.Account).FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public Task<Mentor?> MentorAsync(string email, CancellationToken ct) => db.Mentors.FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public async Task<Student?> ResolveInviteeAsync(string value, CancellationToken ct) =>
        await db.Students.Include(x => x.Account).FirstOrDefaultAsync(x => x.StudentCode == value, ct)
        ?? await db.Students.Include(x => x.Account).FirstOrDefaultAsync(x => x.Account.Email.ToLower() == value.ToLower(), ct);
    public Task<Student?> LockStudentAsync(long id, CancellationToken ct) => db.Students
        .FromSqlInterpolated($"SELECT * FROM students WHERE id = {id} FOR UPDATE").FirstOrDefaultAsync(ct);
    public Task<StudentGroup?> GroupAsync(long id, bool locked, CancellationToken ct)
    {
        var query = locked
            ? db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id = {id} FOR UPDATE")
            : db.StudentGroups.Where(x => x.Id == id);
        // Fresh snapshot after obtaining the lock; earlier request navigation may be tracked.
        return query.AsNoTracking().Include(x => x.TermNavigation)
            .Include(x => x.LeaderStudent).ThenInclude(x => x!.Account)
            .Include(x => x.Mentor).ThenInclude(x => x!.Account).AsSplitQuery().FirstOrDefaultAsync(ct);
    }
    public Task<bool> IsMemberAsync(long groupId, long studentId, CancellationToken ct) =>
        db.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId && x.StudentId == studentId, ct);
    public Task<bool> HasMembershipAsync(long studentId, string term, string course, CancellationToken ct) =>
        db.StudentGroupMembers.AnyAsync(x => x.StudentId == studentId && x.Group.Term == term && x.Group.CourseCode == course, ct);
    public Task<int> MemberCountAsync(long groupId, CancellationToken ct) => db.StudentGroupMembers.CountAsync(x => x.GroupId == groupId, ct);
    private IQueryable<GroupInvitation> Invitations => db.GroupInvitations.Include(x => x.Group)
        .Include(x => x.InviterStudent).Include(x => x.InviteeStudent).ThenInclude(x => x.Account);
    private IQueryable<GroupJoinRequest> Requests => db.GroupJoinRequests.Include(x => x.Group)
        .Include(x => x.Student).ThenInclude(x => x.Account).Include(x => x.RespondedByStudent);
    public Task<GroupInvitation?> InvitationAsync(long id, CancellationToken ct) => Invitations.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<GroupJoinRequest?> RequestAsync(long id, CancellationToken ct) => Requests.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<GroupInvitation>> InvitationsAsync(long? groupId, long? studentId, bool pendingOnly, CancellationToken ct) => Invitations
        .Where(x => (!groupId.HasValue || x.GroupId == groupId) && (!studentId.HasValue || x.InviteeStudentId == studentId) && (!pendingOnly || x.Status == "PENDING"))
        .ToListAsync(ct);
    public Task<List<GroupJoinRequest>> RequestsAsync(long? groupId, long? studentId, bool pendingOnly, CancellationToken ct) => Requests
        .Where(x => (!groupId.HasValue || x.GroupId == groupId) && (!studentId.HasValue || x.StudentId == studentId) && (!pendingOnly || x.Status == "PENDING"))
        .ToListAsync(ct);
    public Task ReloadAsync(object entity, CancellationToken ct) => db.Entry(entity).ReloadAsync(ct);
    public void AddInvitation(GroupInvitation invitation) => db.GroupInvitations.Add(invitation);
    public void AddRequest(GroupJoinRequest request) => db.GroupJoinRequests.Add(request);
    public void AddMember(StudentGroupMember member) => db.StudentGroupMembers.Add(member);
    public async Task AddNotificationAsync(Notification notification, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(x => x.Id == notification.RecipientId && x.Status == "ACTIVE", ct)) return;
        if (await db.Notifications.AnyAsync(x => x.RecipientId == notification.RecipientId && x.EventKey == notification.EventKey, ct)) return;
        db.Notifications.Add(notification);
    }
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
