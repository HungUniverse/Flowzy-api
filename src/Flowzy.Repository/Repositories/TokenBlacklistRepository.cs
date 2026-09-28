using Flowzy.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class TokenBlacklistRepository(FlowzyDbContext db) : ITokenBlacklistRepository
{
    public async Task RevokeAsync(string tokenHash, DateTime expiresAt, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO revoked_access_token(token_hash, expires_at) VALUES ({tokenHash}, {expiresAt})
            ON CONFLICT (token_hash) DO UPDATE
            SET expires_at = GREATEST(revoked_access_token.expires_at, EXCLUDED.expires_at)
            """, ct);

    public Task<bool> IsRevokedAsync(string tokenHash, DateTime now, CancellationToken ct) =>
        db.RevokedAccessTokens.AsNoTracking().AnyAsync(x => x.TokenHash == tokenHash && x.ExpiresAt > now, ct);

    public async Task DeleteExpiredAsync(DateTime now, CancellationToken ct) =>
        await db.RevokedAccessTokens.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(ct);
}
