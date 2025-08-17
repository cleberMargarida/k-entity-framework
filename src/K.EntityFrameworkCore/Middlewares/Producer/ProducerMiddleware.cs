using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer
{
    internal class ProducerMiddleware<T>(Infrastructure<IProducer<string, byte[]>> producer, ProducerMiddlewareOptions<T> options) : Middleware<T>(options)
        where T : class
    {
        private readonly IProducer<string, byte[]> producer = producer.Instance;

        public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            await producer.ProduceAsync(options.TopicName, new()
            {
                Headers = AddHeaders(envelope),
                Key = options.GetKey(envelope.Message!)!,
                Value = ((ISerializedEnvelope<T>)envelope).SerializedData,
            }, cancellationToken);

            await base.InvokeAsync(envelope, cancellationToken);
        }

        private static Headers AddHeaders(Envelope<T> envelope)
        {
            Headers headers = [];
            foreach (var item in ((ISerializedEnvelope<T>)envelope).Headers)
            {
                //TODO make sure that the serializer middleware does serialize headers if they are not serialized
                headers.Add(new(item.Key, (byte[])item.Value));
            }

            return headers;
        }
    }
}
