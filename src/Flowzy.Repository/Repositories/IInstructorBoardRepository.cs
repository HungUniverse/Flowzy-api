using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed record BoardCounts(long Total, long Available, long Mine, long Other);
public sealed record BoardCourseCounts(string Course, BoardCounts Counts);
public interface IInstructorBoardRepository
{
    Task<Instructor?> Instructor(string email, CancellationToken ct);
    Task<(List<StudentGroup> Items, long Total)> Search(long instructor, string term, string course, string search, string assignment, int page, int size, CancellationToken ct);
    Task<BoardCounts> Counts(long instructor, string term, string course, CancellationToken ct);
    Task<List<BoardCourseCounts>> Courses(long instructor, string term, CancellationToken ct);
    Task<IDbContextTransaction> Begin(CancellationToken ct);
    Task<StudentGroup?> LockGroup(long id, CancellationToken ct);
    Task<AcademicTerm?> LockTerm(string code, CancellationToken ct);
    Task<StudentGroup> Detail(long id, CancellationToken ct);
    Task Save(CancellationToken ct);
}
