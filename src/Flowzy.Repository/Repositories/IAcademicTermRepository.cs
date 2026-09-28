using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public interface IAcademicTermRepository
{
    Task<IDbContextTransaction> BeginAsync(CancellationToken ct);
    Task<(List<AcademicTerm> Items, long Total)> PageAsync(int page, int size, CancellationToken ct);
    Task<AcademicTerm?> FindAsync(string code, bool locked, CancellationToken ct);
    Task<AcademicTerm?> LatestOpenAsync(CancellationToken ct);
    Task<Account?> AccountAsync(string email, CancellationToken ct);
    Task<bool> HasGroupsAsync(string code, CancellationToken ct);
    Task<(long Groups, long Expected, long Submitted)> CountsAsync(AcademicTerm term, CancellationToken ct);
    Task<List<StudentGroup>> LockGroupsAsync(string code, CancellationToken ct);
    Task CancelPendingAsync(string code, DateTime now, CancellationToken ct);
    Task<bool> HasFeedbackAsync(long term, long group, long student, string target, CancellationToken ct);
    Task<List<Student>> StudentsAsync(string code, CancellationToken ct);
    Task<HashSet<long>> StudentsInOtherOpenTermsAsync(string code, CancellationToken ct);
    Task AddNotificationAsync(Notification notification, CancellationToken ct);
    void Add(AcademicTerm term);
    void Add(TermFeedback feedback);
    void Remove(AcademicTerm term);
    Task SaveAsync(CancellationToken ct);
}
