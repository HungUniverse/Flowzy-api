using System.Security.Cryptography;
using System.Text;
using Flowzy.Repository.Repositories;

namespace Flowzy.Service.Authentication;

public sealed class TokenBlacklistService(
    IJwtService jwtService, ITokenBlacklistRepository repository, TimeProvider clock) : ITokenBlacklistService
{
    public async Task BlacklistAsync(string token, CancellationToken ct = default)
    {
        var expiresAt = jwtService.GetExpirationUtc(token);
        if (expiresAt > clock.GetUtcNow().UtcDateTime)
            await repository.RevokeAsync(Hash(token), expiresAt, ct);
    }

    // Persist only a digest, never the bearer token itself.
    public Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default) =>
        repository.IsRevokedAsync(Hash(token), clock.GetUtcNow().UtcDateTime, ct);

    public Task DeleteExpiredAsync(CancellationToken ct = default) =>
        repository.DeleteExpiredAsync(clock.GetUtcNow().UtcDateTime, ct);

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
