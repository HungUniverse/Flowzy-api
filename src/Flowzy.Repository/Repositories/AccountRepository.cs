using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class AccountRepository(FlowzyDbContext dbContext) : IAccountRepository
{
    public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Accounts
            .Include(account => account.Instructor)
            .Include(account => account.Mentor)
            .Include(account => account.Student)
            .SingleOrDefaultAsync(account => account.Email.ToLower() == email.Trim().ToLower(), cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Accounts.AnyAsync(account => account.Email.ToLower() == email.Trim().ToLower(), cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default) =>
        await dbContext.Accounts.AddAsync(account, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
