using Flowzy.Service.Contracts;
namespace Flowzy.Service.Backup;
public interface IBackupService
{
    Task<BackupJobResponse> Create(string email, CancellationToken ct);
    Task<RestoreBackupResponse> Restore(Stream stream, string filename, string confirmation, string email, CancellationToken ct);
    Task<PageResponse<BackupJobResponse>> List(string? status, int page, int size, CancellationToken ct);
    Task<BackupJobResponse> Get(long id, CancellationToken ct);
    Task<(string Path, string Name)> Download(long id, CancellationToken ct);
    Task<BackupScheduleResponse> Schedule(CancellationToken ct);
    Task<BackupScheduleResponse> Update(UpdateBackupScheduleRequest request, string email, CancellationToken ct);
    Task FailInterrupted(CancellationToken ct);
    Task RunDueSchedule(CancellationToken ct);
    Task Execute(BackupWorkItem item, CancellationToken ct);
}
