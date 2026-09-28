using System.Threading.Channels;
namespace Flowzy.Service.Backup;
public sealed record BackupWorkItem(long JobId, string Directory, int RetentionDays);
public sealed class BackupQueue
{
    private readonly Channel<BackupWorkItem> channel = Channel.CreateUnbounded<BackupWorkItem>(new() { SingleReader = true });
    public void Enqueue(BackupWorkItem item)
    {
        if (!channel.Writer.TryWrite(item)) throw new InvalidOperationException("Backup queue is closed");
    }
    public IAsyncEnumerable<BackupWorkItem> Read(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
