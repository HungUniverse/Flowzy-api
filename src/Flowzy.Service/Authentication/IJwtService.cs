using System.Security.Claims;
using Flowzy.Repository.Entities;

namespace Flowzy.Service.Authentication;

public interface IJwtService
{
    string GenerateAccessToken(Account account);
    ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true);
    DateTime GetExpirationUtc(string token);
}
