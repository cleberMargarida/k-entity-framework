using Confluent.Kafka;
using K.EntityFrameworkCore.Diagnostics;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Consumer;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

internal class InboxMiddleware<T>(
      ConsumerAssessor<T> consumerAssessor
    , ICurrentDbContext currentDbContext
    , ScopedCommandRegistry scopedCommandRegistry
    , InboxMiddlewareSettings<T> inboxSetting)
    : Middleware<T>(inboxSetting)
    where T : class
{
    private readonly IConsumer consumer = consumerAssessor.Consumer;
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
        var inboxMessages = this.context.Set<InboxMessage>();

        var isDuplicate = (await inboxMessages.FindAsync(new object[] { hashId }, cancellationToken)) != null;
        if (isDuplicate)
        {
            KafkaDiagnostics.InboxDuplicatesFiltered.Add(1);
            return null;
        }

        inboxMessages.Add(new()
        {
            HashId = hashId,
            ExpireAt = DateTime.UtcNow + inboxSetting.DeduplicationTimeWindow,
        });

        scopedCommandRegistry.Add(new CommitMiddlewareInvokeCommand(this.consumer, offset).ExecuteAsync);

        return message;
    }

    private readonly struct CommitMiddlewareInvokeCommand(IConsumer consumer, TopicPartitionOffset topicPartitionOffset)
    {
        public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _ = serviceProvider;
            cancellationToken.ThrowIfCancellationRequested();

            consumer.StoreOffset(new TopicPartitionOffset(
                  topicPartitionOffset.Topic
                , topicPartitionOffset.Partition
                , topicPartitionOffset.Offset + 1
                , topicPartitionOffset.LeaderEpoch));

            return ValueTask.CompletedTask;
        }
    }
}
