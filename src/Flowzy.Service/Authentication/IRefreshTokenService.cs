using Flowzy.Repository.Entities;

namespace Flowzy.Service.Authentication;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(Account account, CancellationToken cancellationToken = default);
    Task<RefreshToken> ValidateAsync(string rawToken, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(long accountId, CancellationToken cancellationToken = default);
}
