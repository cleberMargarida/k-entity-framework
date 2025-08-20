using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific batch middleware that inherits from the base BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerBatchMiddleware<T>(
      Infrastructure<IProducer<string, byte[]>> producer
    , ProducerBatchMiddlewareOptions<T> options
    , ProducerMiddlewareOptions<T> producerOptions)
    : BatchMiddleware<T>(options) where T : class
{
    private readonly IProducer<string, byte[]> producer = producer.Instance;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return base.InvokeAsync(envelope, cancellationToken);
    }

    protected override Task InvokeAsync(ICollection<Envelope<T>> batchToSend, CancellationToken cancellationToken)
    {
        foreach (var envelope in batchToSend)
        {
            ISerializedEnvelope<T> envelopeSerialized = envelope;

            Headers headers = AddHeaders(envelopeSerialized);
            string key = producerOptions.GetKey(envelope.Message!)!;

            Message<string, byte[]> confluentMessage = new()
            {
                Headers = headers,
                Key = key,
                Value = envelopeSerialized.SerializedData,
            };

            producer.Produce(producerOptions.TopicName, confluentMessage);
        }

        return Task.Run(() => producer.Flush(cancellationToken), cancellationToken);
    }

    private static Headers AddHeaders(ISerializedEnvelope<T> envelope)
    {
        Headers headers = [];

        if (envelope.Headers == null)
        {
            return headers;
        }

        foreach (var item in envelope.Headers)
        {
            headers.Add(new(item.Key, (byte[])item.Value));
        }

        return headers;
    }
}
