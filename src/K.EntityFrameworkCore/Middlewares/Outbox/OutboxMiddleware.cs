using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

internal class OutboxMiddleware<T>(OutboxMiddlewareSettings<T> outbox, ICurrentDbContext dbContext) : Middleware<T>(outbox)
    where T : class
{
    private readonly DbContext context = dbContext.Context;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        DbSet<OutboxMessage> outboxMessages = context.Set<OutboxMessage>();

        OutboxMessage message = envelope.AsOutboxMessage();

        outboxMessages.Add(message);

        return outbox.Strategy switch
        {
            OutboxPublishingStrategy.ImmediateWithFallback => base.InvokeAsync(envelope, cancellationToken),
            OutboxPublishingStrategy.BackgroundOnly => ValueTask.CompletedTask,
            _ => throw new NotSupportedException($"The outbox strategy '{outbox.Strategy}' is not supported.")
        };
    }
}
