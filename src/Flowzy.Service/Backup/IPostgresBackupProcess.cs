namespace Flowzy.Service.Backup;

public interface IPostgresBackupProcess
{
    Task Dump(string output, CancellationToken ct);
    Task Restore(string input, CancellationToken ct);
}
