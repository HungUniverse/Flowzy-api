using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Flowzy.Service.Backup;

public sealed class BackupService(IBackupRepository repository, BackupQueue queue, BackupOperationGate gate,
    IPostgresBackupProcess process, IConfiguration configuration, TimeProvider clock) : IBackupService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private string DirectoryPath => string.IsNullOrWhiteSpace(configuration["Backup:Directory"]) ? "/var/backups/fspark/postgres" : configuration["Backup:Directory"]!.Trim();
    private const string StartupInterrupted = "Marked failed on backend startup because the previous backup worker is no longer running.";
    private const string RestoreInterrupted = "Marked failed after database restore because this job state came from the restored dump.";

    public async Task<BackupJobResponse> Create(string email, CancellationToken ct)
    {
        await gate.Admission.WaitAsync(ct);
        try
        {
            if (gate.RestoreInProgress) throw new ConflictException("A database restore is already running");
            var requester = await Requester(email, ct);
            await using var tx = await repository.BeginAdmission(ct);
            var settings = await Settings(ct);
            if (await repository.HasActiveJob(ct)) throw new ConflictException("A backup or restore operation is already queued or running");
            var job = NewJob("MANUAL", requester.Id); repository.Add(job); await repository.Save(ct); await tx.CommitAsync(ct);
            var result = Map(job, requester.Email);
            queue.Enqueue(new(job.Id, DirectoryPath, settings.RetentionDays)); return result;
        }
        finally { gate.Admission.Release(); }
    }

    public async Task<RestoreBackupResponse> Restore(Stream input, string filename, string confirmation, string email, CancellationToken ct)
    {
        if (confirmation != "RESTORE_DATABASE") throw new BadRequestException("Invalid restore confirmation");
        if (input.CanSeek && input.Length == 0) throw new BadRequestException("Backup file is required");
        filename ??= "backup.dump";
        if (!filename.EndsWith(".dump", StringComparison.OrdinalIgnoreCase)) throw new BadRequestException("Only .dump backup files are supported");
        await Requester(email, ct);
        await gate.Admission.WaitAsync(ct);
        try
        {
            if (await repository.HasActiveJob(ct)) throw new ConflictException("A backup job is already queued or running");
            if (gate.RestoreInProgress) throw new ConflictException("A database restore is already running");
            gate.RestoreInProgress = true;
        }
        finally { gate.Admission.Release(); }
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var target = Path.GetFullPath(Path.Combine(DirectoryPath, $"restore-upload-{Now:yyyyMMdd'T'HHmmss'Z'}.dump"));
            await using (var file = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None)) await input.CopyToAsync(file, ct);
            await process.Restore(target, ct);
            await repository.FailInterrupted(RestoreInterrupted, ct);
            return new(Path.GetFileName(target), new FileInfo(target).Length, Now);
        }
        catch (Exception ex) when (ex is not (BadRequestException or ConflictException or NotFoundException or OperationCanceledException))
        { throw new BadRequestException("Database restore failed: " + LimitError(ex.Message)); }
        finally
        {
            await gate.Admission.WaitAsync(CancellationToken.None);
            try { gate.RestoreInProgress = false; } finally { gate.Admission.Release(); }
        }
    }

    public async Task<PageResponse<BackupJobResponse>> List(string? status, int page, int size, CancellationToken ct)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        if (string.IsNullOrEmpty(status)) status = null;
        if (status is not null && status is not ("QUEUED" or "RUNNING" or "SUCCEEDED" or "FAILED")) throw new BadRequestException("Invalid parameter format: status");
        var result = await repository.Jobs(status, page, size, ct);
        return PageResponse<BackupJobResponse>.Create(result.Rows.Select(x => Map(x, x.RequestedByAccount?.Email)).ToList(), page, size, result.Total);
    }
    public async Task<BackupJobResponse> Get(long id, CancellationToken ct)
    {
        var job = await RequireJob(id, ct); return Map(job, job.RequestedByAccount?.Email);
    }
    public async Task<(string Path, string Name)> Download(long id, CancellationToken ct)
    {
        var job = await RequireJob(id, ct);
        if (job.Status != "SUCCEEDED") throw new BadRequestException("Only succeeded backup jobs can be downloaded");
        if (job.FilePath is null || job.FileName is null) throw new NotFoundException("Backup file metadata is missing");
        if (!File.Exists(job.FilePath)) throw new NotFoundException("Backup file not found on server");
        return (Path.GetFullPath(job.FilePath), job.FileName);
    }
    public async Task<BackupScheduleResponse> Schedule(CancellationToken ct)
    {
        await using var tx = await repository.BeginAdmission(ct);
        var settings = await Settings(ct); await tx.CommitAsync(ct); return Map(settings);
    }
    public async Task<BackupScheduleResponse> Update(UpdateBackupScheduleRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.BeginAdmission(ct);
        var settings = await Settings(ct); var requester = await Requester(email, ct);
        var cron = BackupCron.Parse(request.CronExpression); var zone = BackupCron.Zone(request.Timezone);
        if (request.Enabled is null) throw new BadRequestException("Enabled flag is required");
        var retention = request.RetentionDays ?? 14;
        if (retention < 1) throw new BadRequestException("Retention days must be at least 1");
        if (retention > 3650) throw new BadRequestException("Retention days must not exceed 3650");
        settings.Enabled = request.Enabled.Value; settings.CronExpression = request.CronExpression.Trim();
        settings.Timezone = zone.Id; settings.BackupDir = DirectoryPath; settings.RetentionDays = retention;
        settings.UpdatedByAccountId = requester.Id; settings.UpdatedByAccount = requester; settings.UpdatedAt = Now;
        settings.NextRunAt = settings.Enabled ? cron.Next(Now, zone) : null;
        await repository.Save(ct); await tx.CommitAsync(ct); return Map(settings);
    }

    public Task FailInterrupted(CancellationToken ct) => repository.FailInterrupted(StartupInterrupted, ct);
    public async Task RunDueSchedule(CancellationToken ct)
    {
        await gate.Admission.WaitAsync(ct);
        try
        {
            if (gate.RestoreInProgress) return;
            await using var tx = await repository.BeginAdmission(ct);
            var settings = await Settings(ct); var now = Now;
            if (!settings.Enabled || settings.NextRunAt > now) { await tx.CommitAsync(ct); return; }
            var next = BackupCron.Parse(settings.CronExpression).Next(now, BackupCron.Zone(settings.Timezone));
            BackupJob? job = null;
            if (settings.NextRunAt is not null && !await repository.HasActiveJob(ct))
            {
                job = NewJob("SCHEDULED", null); repository.Add(job); settings.LastTriggeredAt = now;
            }
            settings.NextRunAt = next; settings.UpdatedAt = now; await repository.Save(ct); await tx.CommitAsync(ct);
            if (job is not null) queue.Enqueue(new(job.Id, DirectoryPath, settings.RetentionDays));
        }
        finally { gate.Admission.Release(); }
    }

    public async Task Execute(BackupWorkItem item, CancellationToken ct)
    {
        var job = await RequireJob(item.JobId, ct);
        if (job.Status != "QUEUED") return;
        job.Status = "RUNNING"; job.StartedAt = Now; await repository.Save(ct);
        try
        {
            Directory.CreateDirectory(item.Directory);
            var output = Path.GetFullPath(Path.Combine(item.Directory, $"fspark-postgres-{Now:yyyyMMdd'T'HHmmss'Z'}-job-{job.Id}.dump"));
            await process.Dump(output, ct);
            job.Status = "SUCCEEDED"; job.FileName = Path.GetFileName(output); job.FilePath = output;
            job.FileSizeBytes = new FileInfo(output).Length; job.FinishedAt = Now; await repository.Save(ct);
            Cleanup(item.Directory, item.RetentionDays);
        }
        catch (Exception ex)
        {
            job.Status = "FAILED"; job.ErrorMessage = LimitError(ex.Message); job.FinishedAt = Now;
            await repository.Save(CancellationToken.None);
            if (ex is OperationCanceledException) throw;
        }
    }
    private async Task<BackupScheduleSetting> Settings(CancellationToken ct)
    {
        var settings = await repository.Settings(ct);
        if (settings is null)
        {
            settings = new() { Id = 1, Enabled = false, CronExpression = "0 0 2 * * *", Timezone = "Asia/Ho_Chi_Minh",
                BackupDir = DirectoryPath, RetentionDays = 14, UpdatedAt = Now };
            repository.Add(settings); await repository.Save(ct);
        }
        else if (settings.BackupDir != DirectoryPath) { settings.BackupDir = DirectoryPath; settings.UpdatedAt = Now; await repository.Save(ct); }
        return settings;
    }
    private async Task<Account> Requester(string email, CancellationToken ct) => await repository.Requester(email, ct) ?? throw new NotFoundException("Requester account not found");
    private async Task<BackupJob> RequireJob(long id, CancellationToken ct) => await repository.Job(id, ct) ?? throw new NotFoundException($"Backup job not found with id: {id}");
    private BackupJob NewJob(string trigger, long? requesterId) => new() { TriggerType = trigger, Status = "QUEUED", RequestedByAccountId = requesterId, CreatedAt = Now };
    private void Cleanup(string directory, int retentionDays)
    {
        if (retentionDays < 1 || !Directory.Exists(directory)) return;
        var root = Path.GetFullPath(directory); var cutoff = Now.AddDays(-retentionDays);
        try
        {
            foreach (var file in new DirectoryInfo(root).EnumerateFiles("fspark-postgres-*.dump", SearchOption.TopDirectoryOnly).OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                if (!string.Equals(file.DirectoryName, root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                    (file.Attributes & FileAttributes.ReparsePoint) != 0 || file.LastWriteTimeUtc >= cutoff) continue;
                try { file.Delete(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    private static string LimitError(string? message) => string.IsNullOrWhiteSpace(message) ? "Backup job failed" : message.Length > 4000 ? message[..4000] : message;
    private static BackupJobResponse Map(BackupJob job, string? email) => new(job.Id, job.TriggerType, job.Status, job.FileName, job.FileSizeBytes,
        job.ErrorMessage, job.RequestedByAccountId, email, job.StartedAt, job.FinishedAt, job.CreatedAt);
    private BackupScheduleResponse Map(BackupScheduleSetting settings) => new(settings.Enabled, settings.CronExpression, settings.Timezone, DirectoryPath,
        settings.RetentionDays, settings.LastTriggeredAt, settings.NextRunAt, settings.UpdatedByAccountId, settings.UpdatedByAccount?.Email, settings.UpdatedAt);
}
