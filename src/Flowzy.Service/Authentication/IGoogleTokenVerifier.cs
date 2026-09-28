namespace Flowzy.Service.Authentication;

public interface IGoogleTokenVerifier
{
    Task<string> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}
