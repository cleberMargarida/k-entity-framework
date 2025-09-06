using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

[SingletonService]
internal class ConsumerMiddleware<T>(ConsumerMiddlewareSettings<T> settings) : Middleware<T>(settings), IConsumeResultChannel
    where T : class
{
    private readonly Channel<ConsumeResult<string, byte[]>> channel
        = Channel.CreateBounded<ConsumeResult<string, byte[]>>(((IConsumerProcessingConfig)settings).ToBoundedChannelOptions());

    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return InvokeAsync(cancellationToken);
    }

    private async ValueTask<T?> InvokeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await channel.Reader.ReadAsync(cancellationToken);

            scoped var envelope = new Envelope<T>();

            envelope.WeakReference.SetTarget(result.TopicPartitionOffset);

            envelope.Headers = result.Message.Headers.ToImmutableDictionary(h => h.Key, h => System.Text.Encoding.UTF8.GetString(h.GetValueBytes()));
            envelope.Key = result.Message.Key;
            envelope.Payload = result.Message.Value;

            return await base.InvokeAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null!;
        }
    }

    public ValueTask WriteAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellationToken = default)
    {
        return channel.Writer.WriteAsync(result, cancellationToken);
    }
}
