using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;
namespace Flowzy.Repository.Repositories;
public interface IGroupMeetingRepository
{
    Task<IDbContextTransaction> Begin(CancellationToken ct);
    Task<StudentGroup?> Group(long id, bool locked, CancellationToken ct);
    Task<MentorMeeting?> Meeting(long id, CancellationToken ct);
    Task Reload(MentorMeeting meeting, CancellationToken ct);
    Task<Account?> Account(string email, CancellationToken ct);
    Task<Student?> Student(string email, CancellationToken ct);
    Task<Mentor?> Mentor(string email, bool locked, CancellationToken ct);
    Task<Instructor?> Instructor(string email, CancellationToken ct);
    Task<bool> IsMember(long group, long student, CancellationToken ct);
    Task<List<long>> MemberAccounts(long group, CancellationToken ct);
    Task<List<MentorAvailabilitySlot>> Availability(long mentor, DateTime now, CancellationToken ct);
    Task<MentorAvailabilitySlot?> Slot(long id, CancellationToken ct);
    Task<long> ActiveCount(long group, CancellationToken ct);
    Task<bool> Overlap(long mentor, DateTime start, DateTime end, long? exclude, CancellationToken ct);
    Task<List<MentorMeeting>> List(long group, CancellationToken ct);
    Task Notify(Notification notification, CancellationToken ct);
    void Add(MentorMeeting meeting);
    Task Save(CancellationToken ct);
}
