namespace Flowzy.Repository.Repositories;

public interface ITokenBlacklistRepository
{
    Task RevokeAsync(string tokenHash, DateTime expiresAt, CancellationToken ct);
    Task<bool> IsRevokedAsync(string tokenHash, DateTime now, CancellationToken ct);
    Task DeleteExpiredAsync(DateTime now, CancellationToken ct);
}
