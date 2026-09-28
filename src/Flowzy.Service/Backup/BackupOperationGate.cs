namespace Flowzy.Service.Backup;

// Mirrors Java's in-process restore flag; multi-instance restore orchestration is a release gate.
public sealed class BackupOperationGate
{
    public SemaphoreSlim Admission { get; } = new(1, 1);
    public bool RestoreInProgress { get; set; }
}
