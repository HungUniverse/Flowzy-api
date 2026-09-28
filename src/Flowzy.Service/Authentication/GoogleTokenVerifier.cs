using System.Net.Http.Json;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Options;
using Microsoft.Extensions.Options;

namespace Flowzy.Service.Authentication;

public sealed class GoogleTokenVerifier(HttpClient httpClient, IOptions<GoogleOptions> options) : IGoogleTokenVerifier
{
    private readonly GoogleOptions _options = options.Value;

    public async Task<string> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new UnauthorizedException("Google login is not configured");
        }

        GoogleTokenInfoResponse? tokenInfo;
        try
        {
            var uri = $"{_options.TokenInfoUrl}?id_token={Uri.EscapeDataString(idToken)}";
            tokenInfo = await httpClient.GetFromJsonAsync<GoogleTokenInfoResponse>(uri, cancellationToken);
        }
        catch
        {
            throw new UnauthorizedException("Invalid Google token");
        }

        if (tokenInfo is null)
        {
            throw new UnauthorizedException("Invalid Google token");
        }
        if (!string.Equals(tokenInfo.Aud, _options.ClientId, StringComparison.Ordinal))
        {
            throw new UnauthorizedException("Google token audience is invalid");
        }
        if (!IsVerified(tokenInfo.EmailVerified))
        {
            throw new UnauthorizedException("Google email is not verified");
        }
        if (string.IsNullOrWhiteSpace(tokenInfo.Email))
        {
            throw new UnauthorizedException("Google token does not contain an email");
        }
        return tokenInfo.Email.Trim().ToLowerInvariant();
    }

    private static bool IsVerified(object? value) => value switch
    {
        bool boolean => boolean,
        System.Text.Json.JsonElement element when element.ValueKind == System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonElement element when element.ValueKind == System.Text.Json.JsonValueKind.String =>
            bool.TryParse(element.GetString(), out var parsed) && parsed,
        _ => bool.TryParse(value?.ToString(), out var parsed) && parsed
    };
}
