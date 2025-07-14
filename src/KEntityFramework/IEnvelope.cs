using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KEntityFramework;

public interface IEnvelope<out T>
{
    IEnumerable<KeyValuePair<string, string>> Headers { get; }
    object Key { get; }
    T Value { get; }
    string Topic { get; }
    int Partition { get; }
    long Offset { get; }
    long Timestamp { get; }
}

public struct ConsumeContext
{
    public string Topic { get; }
    public int Partition { get; }
    public long Offset { get; }
    public long Timestamp { get; }

    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
    {
        // Implementation for committing the offset
        return new ValueTask();
    }
}