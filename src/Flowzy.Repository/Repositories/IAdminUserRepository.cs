using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IAdminUserRepository
{
    Task<(List<Account> Items, long Total)> SearchAsync(int page, int size, string? search, string? role, string? status, CancellationToken cancellationToken);
    Task<Account?> FindAsync(long id, CancellationToken cancellationToken);
    Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ProfileCodeExistsAsync(string role, string code, long? exceptAccountId, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
