using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

[ScopedService]
internal class OutboxMiddleware<T>(OutboxMiddlewareSettings<T> outbox, ICurrentDbContext dbContext) : Middleware<T>(outbox)
    where T : class
{
    private readonly DbContext context = dbContext.Context;

    public override ValueTask<T?> InvokeAsync(scoped Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        DbSet<OutboxMessage> outboxMessages = context.Set<OutboxMessage>();

        outboxMessages.Add(ConvertToOutbox(envelope));

        return outbox.Strategy switch
        {
            OutboxPublishingStrategy.ImmediateWithFallback => base.InvokeAsync(envelope, cancellationToken),
            OutboxPublishingStrategy.BackgroundOnly => ValueTask.FromResult(default(T)),
            _ => throw new NotSupportedException($"The outbox strategy '{outbox.Strategy}' is not supported.")
        };
    }

    private static OutboxMessage ConvertToOutbox(scoped Envelope<T> envelope)
    {
        Type runtimeType = envelope.Message!.GetType();
        Type compiledType = typeof(T);

        string? runtimeTypeAssemblyQualifiedName;
        if (runtimeType == compiledType)
        {
            runtimeTypeAssemblyQualifiedName = null;
        }
        else
        {
            runtimeTypeAssemblyQualifiedName = envelope.Message.GetType().AssemblyQualifiedName!;
        }

        return new OutboxMessage
        {
            Type = compiledType.AssemblyQualifiedName!,
            RuntimeType = runtimeTypeAssemblyQualifiedName,
            Payload = envelope.Payload.ToArray(),
            Headers = envelope.Headers,
            AggregateId = envelope.Key,
        };
    }
}
