using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Batch;

/// <summary>
/// Producer-specific batch middleware that inherits from the base BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerBatchMiddleware<T>(ProducerBatchMiddlewareSettings<T> settings, ProducerMiddlewareSettings<T> producerSettings, IServiceProvider serviceProvider)
    : BatchMiddleware<T>(settings)
    , IMiddleware<T>
    , IEndMiddleware 
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        cancellationToken.Register(tcs.SetResult);

        Message<string, byte[]> message = new()
        {
            Headers = envelope.GetHeaders(),
            Key = producerSettings.GetKey(envelope.Message!)!,
            Value = envelope.GetSerializedData(),
        };

        var producer = serviceProvider.GetRequiredKeyedService<IBatchProducer>(typeof(T));

        producer.Produce(message.Key, message, HandleDeliveryReport);

        return new ValueTask(tcs.Task);

        void HandleDeliveryReport(DeliveryReport<string, byte[]> report)
        {
            if (report.Error.IsError)
                tcs.SetException(new KafkaException(report.Error));
            else
                tcs.SetResult();
        }
    }
}
