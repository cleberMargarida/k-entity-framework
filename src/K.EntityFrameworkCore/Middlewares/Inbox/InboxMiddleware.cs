using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

internal class InboxMiddleware<T>(
      ICurrentDbContext currentDbContext
    , ScopedCommandRegistry scopedCommandRegistry
    , InboxMiddlewareSettings<T> settings) 
    : Middleware<T>(settings)
    where T : class
{
    private readonly DbContext context = currentDbContext.Context;

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        ulong hashId = settings.Hash(envelope);

        DbSet<InboxMessage> inboxMessages = context.Set<InboxMessage>();

        var isDuplicate = (await inboxMessages.FindAsync(new object[] { hashId }, cancellationToken)) != null;
        if (isDuplicate)
        {
            envelope.Clean();
            return;
        }

        inboxMessages.Add(new()
        {
            HashId = hashId,
            ReceivedAt = DateTime.UtcNow,
        });

        if (envelope.WeakReference.TryGetTarget(out object? target) && target is TopicPartitionOffset offset)
        {
            scopedCommandRegistry.Add(new CommitMiddlewareInvokeCommand(offset).ExecuteAsync);
        }

        await base.InvokeAsync(envelope, cancellationToken);
    }

    readonly struct CommitMiddlewareInvokeCommand(TopicPartitionOffset offset)
    {
        public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var consumer = serviceProvider.GetRequiredService<IConsumer>();
            consumer.StoreOffset(offset);
            return ValueTask.CompletedTask;
        }
    }
}
