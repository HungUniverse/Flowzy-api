namespace Flowzy.Service.Authentication;

public interface ITokenBlacklistService
{
    void Blacklist(string token);
    bool IsBlacklisted(string token);
    void Clear();
}
