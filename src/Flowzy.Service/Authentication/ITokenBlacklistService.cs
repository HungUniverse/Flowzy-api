namespace Flowzy.Service.Authentication;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(string token, CancellationToken ct = default);
    Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
