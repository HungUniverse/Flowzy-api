namespace Flowzy.Service.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; set; } = string.Empty;
    public long ExpirationMs { get; set; } = 3_600_000;
    public long RefreshTokenExpirationMs { get; set; } = 604_800_000;
}

public sealed class AdminOptions
{
    public const string SectionName = "Admin";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class GoogleOptions
{
    public const string SectionName = "Google";
    public string ClientId { get; set; } = string.Empty;
    public string TokenInfoUrl { get; set; } = "https://oauth2.googleapis.com/tokeninfo";
}
