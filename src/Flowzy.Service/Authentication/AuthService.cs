using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Options;
using Microsoft.Extensions.Options;

namespace Flowzy.Service.Authentication;

public sealed class AuthService(
    IAccountRepository accounts,
    IJwtService jwtService,
    IRefreshTokenService refreshTokens,
    ITokenBlacklistService blacklist,
    IGoogleTokenVerifier googleTokenVerifier,
    IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions _jwtOptions = options.Value;

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(request.Email, cancellationToken);
        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password");
        }
        EnsureActive(account);
        return await CompleteLoginAsync(account, cancellationToken);
    }

    public async Task<TokenResponse> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var email = await googleTokenVerifier.VerifyAsync(idToken, cancellationToken);
        var account = await accounts.FindByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedException("Account is not registered in the system");
        EnsureActive(account);
        return await CompleteLoginAsync(account, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await refreshTokens.ValidateAsync(refreshToken, cancellationToken);
        EnsureActive(stored.Account);
        return await CreateTokensAsync(stored.Account, cancellationToken);
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedException("Account not found");
        InstructorProfileDto? instructor = account.Instructor is null ? null : new InstructorProfileDto(
            account.Instructor.Id,
            account.Instructor.InstructorCode,
            account.Instructor.FullName,
            account.Email,
            account.Instructor.Phone,
            account.Instructor.Department,
            account.Instructor.Expertise,
            account.Instructor.Status);
        return new UserInfoResponse(
            account.Id,
            account.Email,
            account.Role,
            account.Status,
            account.MustChangePassword,
            instructor);
    }

    public async Task LogoutAsync(string email, string? accessToken, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken);
        if (account is not null)
        {
            await refreshTokens.RevokeAllAsync(account.Id, cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            blacklist.Blacklist(accessToken);
        }
    }

    private async Task<TokenResponse> CompleteLoginAsync(Account account, CancellationToken cancellationToken)
    {
        account.LastLoginAt = DateTime.UtcNow;
        await accounts.SaveChangesAsync(cancellationToken);
        return await CreateTokensAsync(account, cancellationToken);
    }

    private async Task<TokenResponse> CreateTokensAsync(Account account, CancellationToken cancellationToken) => new(
        jwtService.GenerateAccessToken(account),
        await refreshTokens.CreateAsync(account, cancellationToken),
        "Bearer",
        _jwtOptions.ExpirationMs / 1000);

    private static void EnsureActive(Account account)
    {
        if (account.Status == "LOCKED")
        {
            throw new UnauthorizedException("Account is locked");
        }
        if (account.Status != "ACTIVE")
        {
            throw new UnauthorizedException("Account is not active");
        }
    }
}
