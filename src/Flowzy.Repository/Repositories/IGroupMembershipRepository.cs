using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public interface IGroupMembershipRepository
{
    Task<IDbContextTransaction> BeginAsync(CancellationToken ct);
    Task<Account?> AccountAsync(string email, CancellationToken ct);
    Task<Student?> StudentAsync(string email, CancellationToken ct);
    Task<Mentor?> MentorAsync(string email, CancellationToken ct);
    Task<Student?> ResolveInviteeAsync(string value, CancellationToken ct);
    Task<Student?> LockStudentAsync(long id, CancellationToken ct);
    Task<StudentGroup?> GroupAsync(long id, bool locked, CancellationToken ct);
    Task<bool> IsMemberAsync(long groupId, long studentId, CancellationToken ct);
    Task<bool> HasMembershipAsync(long studentId, string term, string course, CancellationToken ct);
    Task<int> MemberCountAsync(long groupId, CancellationToken ct);
    Task<GroupInvitation?> InvitationAsync(long id, CancellationToken ct);
    Task<GroupJoinRequest?> RequestAsync(long id, CancellationToken ct);
    Task<List<GroupInvitation>> InvitationsAsync(long? groupId, long? studentId, bool pendingOnly, CancellationToken ct);
    Task<List<GroupJoinRequest>> RequestsAsync(long? groupId, long? studentId, bool pendingOnly, CancellationToken ct);
    Task ReloadAsync(object entity, CancellationToken ct);
    void AddInvitation(GroupInvitation invitation);
    void AddRequest(GroupJoinRequest request);
    void AddMember(StudentGroupMember member);
    Task AddNotificationAsync(Notification notification, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
