namespace Flowzy.Repository.Entities;

public sealed class RevokedAccessToken
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
