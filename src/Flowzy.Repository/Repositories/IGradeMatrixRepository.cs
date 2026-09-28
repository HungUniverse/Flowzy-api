using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;
namespace Flowzy.Repository.Repositories;
public interface IGradeMatrixRepository
{
    Task<IDbContextTransaction> Begin(CancellationToken ct);
    Task<Account?> Account(string email, CancellationToken ct);
    Task<Instructor?> Instructor(string email, bool locked, CancellationToken ct);
    Task<Student?> Student(string email, CancellationToken ct);
    Task<CourseMilestone?> Milestone(long id, CancellationToken ct);
    Task<StudentGroup?> Group(long id, bool locked, CancellationToken ct);
    Task<List<StudentGroup>> AssignedGroups(long instructor, string? term, string? course, CancellationToken ct);
    Task<List<CourseMilestone>> OwnedMilestones(long instructor, CancellationToken ct);
    Task<List<CourseMilestone>> ScopeMilestones(string term, string course, CancellationToken ct);
    Task<List<StudentGroupMember>> Members(long group, CancellationToken ct);
    Task<List<MilestoneMemberScore>> Scores(long group, CancellationToken ct);
    Task<List<MilestoneGroupGrade>> Grades(long group, CancellationToken ct);
    Task<List<MilestoneContributionRevision>> Revisions(long group, CancellationToken ct);
    Task<MilestoneContributionRevision?> LockRevision(long milestone, long group, CancellationToken ct);
    Task<List<MilestoneContributionAgreement>> Agreements(long revision, CancellationToken ct);
    void ClearAgreements(IEnumerable<MilestoneContributionAgreement> agreements);
    void Add<T>(T entity) where T : class;
    Task Notify(Notification notification, CancellationToken ct);
    Task Save(CancellationToken ct);
}
