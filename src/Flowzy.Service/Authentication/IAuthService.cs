using Flowzy.Service.Contracts;

namespace Flowzy.Service.Authentication;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<TokenResponse> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserInfoResponse> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default);
    Task LogoutAsync(string email, string? accessToken, CancellationToken cancellationToken = default);
}
