using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class RefreshTokenRepository(FlowzyDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.RefreshTokens
            .Include(token => token.Account)
            .ThenInclude(account => account.Instructor)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task ReplaceForAccountAsync(
        long accountId,
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        await dbContext.RefreshTokens.Where(token => token.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteForAccountAsync(long accountId, CancellationToken cancellationToken = default)
    {
        await dbContext.RefreshTokens.Where(token => token.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
