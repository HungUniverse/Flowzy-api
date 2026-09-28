using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task ReplaceForAccountAsync(long accountId, RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task DeleteForAccountAsync(long accountId, CancellationToken cancellationToken = default);
}
