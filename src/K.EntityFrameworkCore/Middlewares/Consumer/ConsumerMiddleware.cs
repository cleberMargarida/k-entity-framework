using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddleware<T>(ConsumerMiddlewareSettings<T> settings) : Middleware<T>(settings), IConsumeResultChannel
    where T : class
{
    private readonly Channel<ConsumeResult<string, byte[]>> channel = Channel
        .CreateBounded<ConsumeResult<string, byte[]>>(((IConsumerProcessingConfig)settings).ToBoundedChannelOptions());

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await channel.Reader.ReadAsync(cancellationToken);

            envelope.WeakReference.SetTarget(result.TopicPartitionOffset);

            FillEnvelopeWithConsumeResult(envelope, result);

            await base.InvokeAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
    }

    private static void FillEnvelopeWithConsumeResult(ISerializedEnvelope<T> envelope, ConsumeResult<string, byte[]> result)
    {
        envelope.Headers = result.Message.Headers.ToDictionary(h => h.Key, h => (object)h.GetValueBytes());
        envelope.Key = result.Message.Key;
        envelope.SerializedData = result.Message.Value;
    }

    public ValueTask WriteAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellationToken = default)
    {
        return channel.Writer.WriteAsync(result, cancellationToken);
    }
}
