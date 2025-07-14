using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KEntityFramework;

public interface IProducer<T>
{
    Task ProduceAsync(T message);
    Task ProduceAsync(T message, CancellationToken cancellationToken);
    Task ProduceAsync(T message, IEnumerable<KeyValuePair<string, string>> headers);
    Task ProduceAsync(T message, IEnumerable<KeyValuePair<string, string>> headers, CancellationToken cancellationToken);
}
