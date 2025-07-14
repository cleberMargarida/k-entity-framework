using System.Threading;
using System.Threading.Tasks;

namespace KEntityFramework
{
    public interface IConsumerHandler<T>
    {
        ValueTask HandleAsync(T message, ConsumeContext context, CancellationToken cancellationToken = default);
    }
}