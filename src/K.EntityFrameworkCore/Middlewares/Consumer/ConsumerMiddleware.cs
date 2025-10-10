using K.EntityFrameworkCore.Middlewares.Core;
using System.Collections.Immutable;
using System.Text.Json;

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

            scoped var envelope = new Envelope<T>();

            envelope.WeakReference.SetTarget(result.TopicPartitionOffset);

            var header = result.Message.Headers[^1];

            envelope.Headers = header.Key == "__debezium.outbox.headers"
                ? JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(header.GetValueBytes())
                : result.Message.Headers.ToImmutableDictionary(h => h.Key, h => System.Text.Encoding.UTF8.GetString(h.GetValueBytes()));

            envelope.Key = result.Message.Key;
            envelope.Payload = result.Message.Value;

            return await base.InvokeAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null!;
        }
    }
}
