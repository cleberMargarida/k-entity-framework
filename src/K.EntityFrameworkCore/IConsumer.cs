using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KEntityFramework;

public interface IConsumer<T> : IAsyncEnumerable<T>
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(long offset, CancellationToken cancellationToken = default);
}
