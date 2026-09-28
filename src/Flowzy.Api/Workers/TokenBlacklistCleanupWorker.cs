using Flowzy.Service.Authentication;

namespace Flowzy.Api.Workers;

public sealed class TokenBlacklistCleanupWorker(IServiceScopeFactory scopes, ILogger<TokenBlacklistCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>().DeleteExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Expired token cleanup failed; revocations remain in the database"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
