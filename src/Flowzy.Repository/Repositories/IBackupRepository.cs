using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public interface IBackupRepository
{
    Task<IDbContextTransaction> BeginAdmission(CancellationToken ct);
    Task<Account?> Requester(string email, CancellationToken ct);
    Task<bool> HasActiveJob(CancellationToken ct);
    Task<BackupJob?> Job(long id, CancellationToken ct);
    Task<(List<BackupJob> Rows, long Total)> Jobs(string? status, int page, int size, CancellationToken ct);
    Task<BackupScheduleSetting?> Settings(CancellationToken ct);
    void Add(BackupJob job);
    void Add(BackupScheduleSetting settings);
    Task Save(CancellationToken ct);
    Task FailInterrupted(string message, CancellationToken ct);
}
