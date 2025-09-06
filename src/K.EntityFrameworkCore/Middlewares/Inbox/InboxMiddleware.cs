using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Consumer;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

[ScopedService]
internal class InboxMiddleware<T>(
      ICurrentDbContext currentDbContext
    , ScopedCommandRegistry scopedCommandRegistry
    , InboxMiddlewareSettings<T> inboxSetting)
    : Middleware<T>(inboxSetting)
    where T : class
{
    private readonly DbContext context = currentDbContext.Context;

    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        T message = envelope.Message;
        envelope.WeakReference.TryGetTarget(out object? target);
        var offset = target as TopicPartitionOffset ?? throw new InvalidOperationException("Topic partition offset not stored.");

        return DeduplicateAsync(message, offset, cancellationToken);
    }

    private async ValueTask<T?> DeduplicateAsync(T message, TopicPartitionOffset offset, CancellationToken cancellationToken)
    {
        ulong hashId = inboxSetting.Hash(message);
        var inboxMessages = context.Set<InboxMessage>();

        var isDuplicate = (await inboxMessages.FindAsync(new object[] { hashId }, cancellationToken)) != null;
        if (isDuplicate)
        {
            return null;
        }

        inboxMessages.Add(new()
        {
            HashId = hashId,
            ExpireAt = DateTime.UtcNow + inboxSetting.DeduplicationTimeWindow,
        });

        scopedCommandRegistry.Add(new CommitMiddlewareInvokeCommand(offset).ExecuteAsync);

        return message;
    }

    private readonly struct CommitMiddlewareInvokeCommand(TopicPartitionOffset topicPartitionOffset)
    {
        public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            // We need to resolve settings again to choose the correct consumer
            var settings = serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>();

            IConsumer consumer;

            if (settings.ExclusiveConnection)
            {
                //var consumerC = serviceProvider.GetRequiredKeyedService<ConsumerConfig>(typeof(T));
                consumer = serviceProvider.GetRequiredKeyedService<IConsumer>(typeof(T));
            }
            else
            {
                consumer = serviceProvider.GetRequiredService<IConsumer>();
            }

            consumer.StoreOffset(new TopicPartitionOffset(topicPartitionOffset.Topic, topicPartitionOffset.Partition, topicPartitionOffset.Offset + 1, topicPartitionOffset.LeaderEpoch));
            return ValueTask.CompletedTask;
        }
    }
}
