using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific batch middleware that inherits from the base BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerBatchMiddleware<T>(ProducerBatchMiddlewareSettings<T> settings, ProducerMiddlewareSettings<T> producerSettings, IServiceProvider serviceProvider)
    : BatchMiddleware<T>(settings) where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        Type type = typeof(T);
        var reference = envelope.GetInfrastructure();
        var options = serviceProvider.GetRequiredService<ProducerMiddlewareSettings<T>>();

        Message<string, byte[]> message;
        if (!reference.TryGetTarget(out OutboxMessage? outboxMessage) && outboxMessage != null)
        {
            //TODO: avoid box -> unbox -> box -> unbox -> box
            message = new Message<string, byte[]>
            {
                //Headers = ,
                Key = outboxMessage.AggregateId!,
                Value = outboxMessage.Payload,
            };
        }
        else
        {
            var serializedEnvelope = envelope as ISerializedEnvelope<T>;
            message = new Message<string, byte[]>
            {
                //Headers = ,
                Key = producerSettings.GetKey(envelope.Message!)!,
                Value = serializedEnvelope.SerializedData
            };
        }

        var producer = serviceProvider.GetRequiredKeyedService<IBatchProducer>(type);

        producer.Produce(message.Key, message, HandleDeliveryReport);

        return base.InvokeAsync(envelope, cancellationToken);
    }

    private void HandleDeliveryReport(DeliveryReport<string, byte[]> report)
    {
        throw new NotImplementedException();
    }
}
