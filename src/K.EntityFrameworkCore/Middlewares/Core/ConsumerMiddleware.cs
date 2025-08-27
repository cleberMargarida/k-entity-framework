using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading.Channels;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddleware<T>(
      ConsumerMiddlewareSettings<T> settings,
      KafkaConsumerPollService pollService,
      KafkaConsumerChannelOptions channelOptions)
    : Middleware<T>(settings)
    , IConsumeResultChannel
    where T : class
{
    private readonly KafkaConsumerPollService _ = pollService;

    private readonly Channel<ConsumeResult<string, byte[]>> channel = Channel.CreateBounded<ConsumeResult<string, byte[]>>(channelOptions.ToBoundedChannelOptions());

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await channel.Reader.ReadAsync(cancellationToken);

            envelope.WeakReference.SetTarget(result.TopicPartitionOffset);
            FillEnvelopeWithConsumeResult(envelope, result);

            await base.InvokeAsync(envelope, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("completed"))
        {
            throw;
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

    public bool TryEnqueue(ConsumeResult<string, byte[]> result)
    {
        var success = channel.Writer.TryWrite(result);
        return success;
    }

    public async ValueTask WriteAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellationToken = default)
    {
        try
        {
            await channel.Writer.WriteAsync(result, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("closed"))
        {
            throw;
        }
    }

    public async ValueTask<ConsumeResult<string, byte[]>> ReadAsync(CancellationToken cancellationToken = default)
    {
        return await channel.Reader.ReadAsync(cancellationToken);
    }
}

/// <summary>
/// Legacy interface replaced by IConsumeResultChannel.
/// Kept for backward compatibility but should not be used in new code.
/// </summary>
[Obsolete("Use IConsumeResultChannel instead. This interface will be removed in a future version.")]
interface IConsumeResultSource
{
    void SetResult(ConsumeResult<string, byte[]> result);
}