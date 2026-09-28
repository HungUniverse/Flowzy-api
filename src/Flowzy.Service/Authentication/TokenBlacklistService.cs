using System.Collections.Concurrent;

namespace Flowzy.Service.Authentication;

public sealed class TokenBlacklistService(IJwtService jwtService) : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

    public void Blacklist(string token)
    {
        DateTime expiresAt;
        try
        {
            expiresAt = jwtService.GetExpirationUtc(token);
        }
        catch
        {
            expiresAt = DateTime.UtcNow.AddHours(1);
        }

        if (expiresAt > DateTime.UtcNow)
        {
            _blacklist[token] = expiresAt;
        }
    }

    public bool IsBlacklisted(string token)
    {
        if (!_blacklist.TryGetValue(token, out var expiration))
        {
            return false;
        }
        if (expiration <= DateTime.UtcNow)
        {
            _blacklist.TryRemove(token, out _);
            return false;
        }
        return true;
    }

    public void Clear() => _blacklist.Clear();
}
