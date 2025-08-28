using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Producer
{
    internal class ProducerMiddleware<T>(IProducer producer, ICurrentDbContext dbContext, ProducerMiddlewareSettings<T> settings)
        : Middleware<T>(settings)
        where T : class
    {
        private readonly DbContext context = dbContext.Context;

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

            if (envelope.WeakReference.TryGetTarget(out var outbox))
            {
                context.Entry(outbox).State = EntityState.Detached;
            }
        }
    }
}
