using Flowzy.Service.Backup;
namespace Flowzy.Api.Workers;
public sealed class BackupWorker(BackupQueue queue, IServiceScopeFactory scopes, TimeProvider clock,
    IConfiguration configuration, ILogger<BackupWorker> log) : BackgroundService
{
    public override async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBackupService>().FailInterrupted(ct);
        await base.StartAsync(ct);
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.WhenAll(RunJobs(stoppingToken), RunSchedule(stoppingToken));
    private async Task RunJobs(CancellationToken ct)
    {
        await foreach (var item in queue.Read(ct))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IBackupService>().Execute(item, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogError(ex, "Backup job {JobId} failed", item.JobId); }
        }
    }
    private async Task RunSchedule(CancellationToken ct)
    {
        if (!configuration.GetValue("Backup:SchedulerEnabled", true)) return;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IBackupService>().RunDueSchedule(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogError(ex, "Scheduled backup check failed"); }
            await Task.Delay(TimeSpan.FromMinutes(1), clock, ct);
        }
    }
}
