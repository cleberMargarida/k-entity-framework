using Confluent.Kafka;

namespace K.EntityFrameworkCore.Middlewares.Core
{
    internal class ProducerMiddleware<T>(IProducer producer, ProducerMiddlewareSettings<T> settings)
        : Middleware<T>(settings)
        where T : class
    {
        public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            Message<string, byte[]> confluentMessage = new()
            {
                Headers = envelope.GetHeaders(),
                Key = settings.GetKey(envelope.Message!)!,
                Value = envelope.GetSerializedData(),
            };

            await producer.ProduceAsync(settings.TopicName, confluentMessage, cancellationToken);

            await base.InvokeAsync(envelope, cancellationToken);
        }
    }
}
