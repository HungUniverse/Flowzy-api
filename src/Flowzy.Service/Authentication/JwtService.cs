using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Flowzy.Repository.Entities;
using Flowzy.Service.Options;

namespace Flowzy.Service.Authentication;

public sealed class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(Account account)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, account.Email),
                new Claim("role", account.Role)
            ]),
            IssuedAt = now,
            Expires = now.AddMilliseconds(_options.ExpirationMs),
            SigningCredentials = new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return handler.ValidateToken(token, CreateValidationParameters(validateLifetime), out _);
    }

    public DateTime GetExpirationUtc(string token)
    {
        var principal = ValidateToken(token, false);
        var exp = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
            ?? throw new SecurityTokenException("Token has no expiration");
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;
    }

    public TokenValidationParameters CreateValidationParameters(bool validateLifetime = true) => new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = GetKey(),
        ValidateLifetime = validateLifetime,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role"
    };

    private SymmetricSecurityKey GetKey()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("JWT secret key is not configured");
        }
        return new SymmetricSecurityKey(Convert.FromBase64String(_options.SecretKey));
    }
}
