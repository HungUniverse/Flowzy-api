using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed class BackupRepository(FlowzyDbContext db) : IBackupRepository
{
    public async Task<IDbContextTransaction> BeginAdmission(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);
        try { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78362420)", ct); return tx; }
        catch { await tx.DisposeAsync(); throw; }
    }
    public Task<Account?> Requester(string email, CancellationToken ct) => db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), ct);
    public Task<bool> HasActiveJob(CancellationToken ct) => db.BackupJobs.AnyAsync(x => x.Status == "QUEUED" || x.Status == "RUNNING", ct);
    public Task<BackupJob?> Job(long id, CancellationToken ct) => db.BackupJobs.Include(x => x.RequestedByAccount).FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<(List<BackupJob> Rows, long Total)> Jobs(string? status, int page, int size, CancellationToken ct)
    {
        var query = db.BackupJobs.AsNoTracking().Include(x => x.RequestedByAccount).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        var total = await query.LongCountAsync(ct);
        return (await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip(page * size).Take(size).ToListAsync(ct), total);
    }
    public Task<BackupScheduleSetting?> Settings(CancellationToken ct) => db.BackupScheduleSettings.Include(x => x.UpdatedByAccount).FirstOrDefaultAsync(x => x.Id == 1, ct);
    public void Add(BackupJob job) => db.BackupJobs.Add(job);
    public void Add(BackupScheduleSetting settings) => db.BackupScheduleSettings.Add(settings);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public async Task FailInterrupted(string message, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE backup_jobs SET status='FAILED', error_message={message}, finished_at=CURRENT_TIMESTAMP WHERE status IN ('QUEUED','RUNNING')", ct);
    }
}
