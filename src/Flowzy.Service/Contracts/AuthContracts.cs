using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Flowzy.Service.Contracts;

public sealed record LoginRequest(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,
    [Required(ErrorMessage = "Password is required")]
    string Password);

public sealed record GoogleLoginRequest(
    [Required(ErrorMessage = "Google ID token is required")]
    string IdToken);

public sealed record RefreshTokenRequest(
    [Required(ErrorMessage = "Refresh token is required")]
    string RefreshToken);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    long ExpiresIn);

public sealed record InstructorProfileDto(
    long Id,
    string InstructorCode,
    string FullName,
    string Email,
    string? Phone,
    string? Department,
    string? Expertise,
    string Status);

public sealed record UserInfoResponse(
    long Id,
    string Email,
    string Role,
    string Status,
    bool MustChangePassword,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    InstructorProfileDto? InstructorProfile);

internal sealed record GoogleTokenInfoResponse(
    string? Aud,
    string? Email,
    [property: JsonPropertyName("email_verified")]
    object? EmailVerified);
