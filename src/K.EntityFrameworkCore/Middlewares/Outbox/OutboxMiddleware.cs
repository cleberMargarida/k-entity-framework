using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

internal class OutboxMiddleware<T>(OutboxMiddlewareSettings<T> outbox, ProducerMiddlewareSettings<T> producerMiddlewareSettings, ICurrentDbContext dbContext) : Middleware<T>(outbox)
    where T : class
{
    private readonly string topicName = producerMiddlewareSettings.TopicName;
    private readonly DbContext context = dbContext.Context;

    public override ValueTask<T?> InvokeAsync(scoped Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        DbSet<OutboxMessage> outboxMessages = this.context.Set<OutboxMessage>();

        outboxMessages.Add(ConvertToOutbox(envelope));

        return outbox.Strategy switch
        {
            OutboxPublishingStrategy.ImmediateWithFallback => base.InvokeAsync(envelope, cancellationToken),
            OutboxPublishingStrategy.BackgroundOnly => ValueTask.FromResult(default(T)),
            _ => throw new NotSupportedException($"The outbox strategy '{outbox.Strategy}' is not supported.")
        };
    }

    private OutboxMessage ConvertToOutbox(scoped Envelope<T> envelope)
    {
        Type runtimeType = envelope.Message!.GetType();
        Type compiledType = typeof(T);

        string? runtimeTypeAssemblyQualifiedName = runtimeType != compiledType
            ? envelope.Message.GetType().AssemblyQualifiedName!
            : null;

        return new OutboxMessage
        {
            Type = compiledType.AssemblyQualifiedName!,
            RuntimeType = runtimeTypeAssemblyQualifiedName,
            Topic = this.topicName,
            Headers = envelope.Headers,
            AggregateId = envelope.Key,
            Payload = envelope.Payload.ToArray(),
        };
    }
}
