using Flowzy.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public interface IDatabaseStatusRepository
{
    Task<bool> CanConnectAsync(CancellationToken ct);
}

public sealed class DatabaseStatusRepository(FlowzyDbContext db) : IDatabaseStatusRepository
{
    public Task<bool> CanConnectAsync(CancellationToken ct) => db.Database.CanConnectAsync(ct);
}
