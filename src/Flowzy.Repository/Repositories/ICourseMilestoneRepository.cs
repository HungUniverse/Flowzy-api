using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;
namespace Flowzy.Repository.Repositories;
public interface ICourseMilestoneRepository
{
    Task<IDbContextTransaction> Begin(CancellationToken ct);
    Task<Account?> Account(string email, CancellationToken ct);
    Task<Instructor?> Instructor(string email, bool locked, CancellationToken ct);
    Task<Student?> Student(string email, CancellationToken ct);
    Task<AcademicTerm?> Term(string code, CancellationToken ct);
    Task<List<StudentGroup>> AssignedGroups(long instructor, string term, string course, CancellationToken ct);
    Task<List<StudentGroup>> StudentGroups(long student, CancellationToken ct);
    Task<StudentGroup?> Group(long id, CancellationToken ct);
    Task<CourseMilestone?> Get(long id, CancellationToken ct);
    Task<List<CourseMilestone>> Owned(long instructor, CancellationToken ct);
    Task<List<CourseMilestone>> Scope(long instructor, string term, string course, CancellationToken ct);
    Task<bool> HasHigherGrade(long id, decimal max, CancellationToken ct);
    Task<List<MilestoneSubmission>> Submissions(long id, CancellationToken ct);
    Task Notify(Notification notice, CancellationToken ct);
    void Add(CourseMilestone milestone);
    Task Save(CancellationToken ct);
    Task Reload(CourseMilestone milestone, CancellationToken ct);
}
