using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IStudentDiscoveryRepository
{
    Task<Student?> FindActiveByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<(List<Student> Items, long Total)> FindUngroupedAsync(string term, string courseCode, string? search,
        int page, int size, CancellationToken cancellationToken = default);
}
