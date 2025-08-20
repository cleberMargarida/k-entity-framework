using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Core
{
    internal class ProducerMiddleware<T>(IProducer producer, ProducerMiddlewareSettings<T> settings) : Middleware<T>(settings)
        where T : class
    {
        public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            ISerializedEnvelope<T> envelopeSerialized = envelope;

            Headers headers = AddHeaders(envelopeSerialized);
            string key = settings.GetKey(envelope.Message!)!;

            Message<string, byte[]> confluentMessage = new()
            {
                Headers = headers,
                Key = key,
                Value = envelopeSerialized.SerializedData,
            };

            await producer.ProduceAsync(settings.TopicName, confluentMessage, cancellationToken);

            await base.InvokeAsync(envelope, cancellationToken);
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
}
