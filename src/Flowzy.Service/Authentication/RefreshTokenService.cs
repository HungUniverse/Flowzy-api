using System.Security.Cryptography;
using System.Text;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Options;
using Microsoft.Extensions.Options;

namespace Flowzy.Service.Authentication;

public sealed class RefreshTokenService(
    IRefreshTokenRepository repository,
    IOptions<JwtOptions> options) : IRefreshTokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<string> CreateAsync(Account account, CancellationToken cancellationToken = default)
    {
        var rawToken = Guid.NewGuid().ToString();
        await repository.ReplaceForAccountAsync(account.Id, new RefreshToken
        {
            AccountId = account.Id,
            Account = account,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_options.RefreshTokenExpirationMs),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        return rawToken;
    }

    public async Task<RefreshToken> ValidateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var token = await repository.FindByHashAsync(Hash(rawToken), cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token");
        if (token.ExpiresAt.ToUniversalTime() < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token has expired");
        }
        if (token.RevokedAt is not null)
        {
            throw new UnauthorizedException("Refresh token has been revoked");
        }
        return token;
    }

    public Task RevokeAllAsync(long accountId, CancellationToken cancellationToken = default) =>
        repository.DeleteForAccountAsync(accountId, cancellationToken);

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
