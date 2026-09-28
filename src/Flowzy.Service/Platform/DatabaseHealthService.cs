using Flowzy.Repository.Repositories;

namespace Flowzy.Service.Platform;

public interface IDatabaseHealthService
{
    Task<bool> IsAvailableAsync(CancellationToken ct);
}

public sealed class DatabaseHealthService(IDatabaseStatusRepository repository) : IDatabaseHealthService
{
    public Task<bool> IsAvailableAsync(CancellationToken ct) => repository.CanConnectAsync(ct);
}
