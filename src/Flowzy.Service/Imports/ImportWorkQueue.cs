using System.Threading.Channels;

namespace Flowzy.Service.Imports;

public sealed record ImportWorkItem(long BatchId, string FilePath);
public sealed class ImportWorkQueue
{
    private readonly Channel<ImportWorkItem> channel = Channel.CreateUnbounded<ImportWorkItem>(new UnboundedChannelOptions { SingleReader = true });
    public SemaphoreSlim Admission { get; } = new(1, 1);
    public void Enqueue(ImportWorkItem item) { if (!channel.Writer.TryWrite(item)) throw new InvalidOperationException("Import worker unavailable"); }
    public IAsyncEnumerable<ImportWorkItem> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
