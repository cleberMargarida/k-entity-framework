using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Producer;

[ScopedService]
internal class ProducerMiddleware<T>(IProducer producer, ICurrentDbContext dbContext, ProducerMiddlewareSettings<T> settings)
    : Middleware<T>(settings)
    where T : class
{
    private readonly DbContext context = dbContext.Context;

    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        Message<string, byte[]> confluentMessage = new()
        {
            Headers = envelope.Headers.ToConfluentHeaders(),
            Key = envelope.Key,
            Value = envelope.Payload.ToArray(),
        };

        return ProduceAsync(confluentMessage, envelope.WeakReference, cancellationToken);
    }

    private async ValueTask<T?> ProduceAsync(Message<string, byte[]> confluentMessage, WeakReference<object> weakReference, CancellationToken cancellationToken)
    {
        await producer.ProduceAsync(settings.TopicName, confluentMessage, cancellationToken);

        if (weakReference.TryGetTarget(out var outbox))
            this.context.Entry(outbox).State = EntityState.Detached;

        return null;
    }
}
