using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IAdminGroupRepository
{
    Task<(List<StudentGroup> Items, long Total)> SearchAsync(int page, int size, string? search, string? status,
        CancellationToken cancellationToken = default);
}
