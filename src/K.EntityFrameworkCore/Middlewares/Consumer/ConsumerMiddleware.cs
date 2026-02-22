using K.EntityFrameworkCore.Middlewares.Core;
using System.Collections.Immutable;
using System.Text;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddleware<T>(Channel<T> channel, ConsumerMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return InvokeAsync(cancellationToken);
    }

    private async ValueTask<T?> InvokeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await channel.ReadAsync(cancellationToken);

            scoped Envelope<T> envelope = default;

            envelope.WeakReference.SetTarget(result.TopicPartitionOffset);

            envelope.Headers = result.Message.Headers
                .ToImmutableDictionary(
                    h => h.Key,
                    h =>
                    {
                        var bytes = h.GetValueBytes();
                        return bytes is null ? string.Empty : Encoding.UTF8.GetString(bytes);
                    });
            envelope.Key = result.Message.Key;
            envelope.Payload = result.Message.Value;

            return await base.InvokeAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
