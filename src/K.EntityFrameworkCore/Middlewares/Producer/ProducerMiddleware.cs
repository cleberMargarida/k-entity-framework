using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer
{
    internal class ProducerMiddleware<T>(IProducer producer, ProducerMiddlewareOptions<T> options) : Middleware<T>(options)
        where T : class
    {
        public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            ISerializedEnvelope<T> envelopeSerialized = envelope;

            Headers headers = AddHeaders(envelopeSerialized);
            string key = options.GetKey(envelope.Message!)!;

            Message<string, byte[]> confluentMessage = new() 
            {
                Headers = headers, 
                Key = key, 
                Value = envelopeSerialized.SerializedData, 
            };

            await producer.ProduceAsync(options.TopicName, confluentMessage, cancellationToken);

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
