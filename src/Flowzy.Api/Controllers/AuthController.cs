using System.IdentityModel.Tokens.Jwt;
using Flowzy.Service.Authentication;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<TokenResponse>.Success(tokens, "Login successful"));
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Google(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await authService.LoginWithGoogleAsync(request.IdToken, cancellationToken);
        return Ok(ApiResponse<TokenResponse>.Success(tokens, "Google login successful"));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return Ok(ApiResponse<TokenResponse>.Success(tokens, "Token refreshed successfully"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserInfoResponse>>> Me(CancellationToken cancellationToken)
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name
            ?? throw new UnauthorizedException("Not authenticated");
        var user = await authService.GetCurrentUserAsync(email, cancellationToken);
        return Ok(ApiResponse<UserInfoResponse>.Success(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken)
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.Identity?.Name ?? string.Empty;
        var header = Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.Ordinal) ? header[7..] : null;
        await authService.LogoutAsync(email, token, cancellationToken);
        return Ok(ApiResponse<object>.Success(null, "Logged out successfully"));
    }
}
