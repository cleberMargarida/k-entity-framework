using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Producer;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace K.EntityFrameworkCore.Extensions;

internal delegate ValueTask CommandExecutor(object? arg0, object? arg1, IServiceProvider serviceProvider, CancellationToken cancellationToken);

internal delegate void SyncCommandExecutor(object? arg0, object? arg1, IServiceProvider serviceProvider);

internal struct CommandEntry
{
    public CommandExecutor Executor;
    public SyncCommandExecutor? SyncExecutor;
    public object? Arg0;
    public object? Arg1;
}

[InlineArray(4)]
internal struct CommandBuffer
{
    private CommandEntry element0;
}

internal class ScopedCommandRegistry
{
    private CommandBuffer buffer;
    private int count;
    private List<CommandEntry>? overflow;

    static class ProducerExecutorCache<T> where T : class
    {
        public static readonly CommandExecutor Instance = static async (msg, _, sp, ct) =>
        {
            await sp.GetRequiredService<ProducerMiddlewareInvoker<T>>()
                    .InvokeAsync(new Envelope<T>((T)msg!), ct);
        };
    }

    static readonly CommandExecutor s_commitExecutor = static (consumer, offset, _, ct) =>
    {
        ct.ThrowIfCancellationRequested();
        var tpo = (TopicPartitionOffset)offset!;
        ((IConsumer)consumer!).StoreOffset(new TopicPartitionOffset(
            tpo.Topic, tpo.Partition, tpo.Offset + 1, tpo.LeaderEpoch));
        return ValueTask.CompletedTask;
    };

    static class ProducerSyncExecutorCache<T> where T : class
    {
        public static readonly SyncCommandExecutor Instance = Execute;

        static void Execute(object? msg, object? _, IServiceProvider sp)
        {
            var model = sp.GetRequiredService<ICurrentDbContext>().Context.Model;
            var strategy = model.GetProducerForgetStrategy<T>();

            if (strategy != ForgetStrategy.FireForget)
            {
                throw new InvalidOperationException(
                    $"Synchronous SaveChanges() is not supported for producer of type '{typeof(T).Name}' " +
                    $"unless FireForget strategy is enabled. Configure .HasForget(f => f.UseFireForget()) " +
                    $"on the producer or use SaveChangesAsync() instead. [PERF-005]");
            }

            var producerSettings = sp.GetRequiredService<ProducerMiddlewareSettings<T>>();
            var serializationSettings = sp.GetRequiredService<SerializationMiddlewareSettings<T>>();
            T message = (T)msg!;

            var key = producerSettings.GetKey(message);
            var headers = producerSettings.GetHeaders(message);
            byte[] payload = serializationSettings.Serializer.Serialize(headers, message).ToArray();

            var confluentMessage = new Message<string, byte[]>
            {
                Headers = headers.ToConfluentHeaders(),
                Key = key,
                Value = payload,
            };

            var producer = sp.GetRequiredService<IProducer>();
            var logger = sp.GetService<ILogger<ScopedCommandRegistry>>();

            producer.Produce(producerSettings.TopicName, confluentMessage, deliveryReport =>
            {
                if (deliveryReport.Error.IsError)
                {
                    logger?.LogError(
                        "Kafka sync delivery failure for {Type} on {Topic}: {Reason} [PERF-005]",
                        typeof(T).Name, deliveryReport.Topic, deliveryReport.Error.Reason);
                }
            });
        }
    }

    static readonly SyncCommandExecutor s_commitSyncExecutor = static (consumer, offset, _) =>
    {
        var tpo = (TopicPartitionOffset)offset!;
        ((IConsumer)consumer!).StoreOffset(new TopicPartitionOffset(
            tpo.Topic, tpo.Partition, tpo.Offset + 1, tpo.LeaderEpoch));
    };

    public void AddProduce<T>(T message) where T : class
    {
        var entry = new CommandEntry
        {
            Executor = ProducerExecutorCache<T>.Instance,
            SyncExecutor = ProducerSyncExecutorCache<T>.Instance,
            Arg0 = message
        };
        AddEntry(entry);
    }

    public void AddCommit(IConsumer consumer, TopicPartitionOffset offset)
    {
        var entry = new CommandEntry
        {
            Executor = s_commitExecutor,
            SyncExecutor = s_commitSyncExecutor,
            Arg0 = consumer,
            Arg1 = offset
        };
        AddEntry(entry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEntry(CommandEntry entry)
    {
        if (this.count < 4)
        {
            this.buffer[this.count] = entry;
            this.count++;
        }
        else
        {
            this.overflow ??= new List<CommandEntry>();
            this.overflow.Add(entry);
        }
    }

    public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        try
        {
            int inlineCount = Math.Min(this.count, 4);
            for (int i = 0; i < inlineCount; i++)
            {
                var entry = this.buffer[i];
                await entry.Executor(entry.Arg0, entry.Arg1, serviceProvider, cancellationToken);
            }

            if (this.overflow is { Count: > 0 } overflow)
            {
                for (int i = 0; i < overflow.Count; i++)
                {
                    var entry = overflow[i];
                    await entry.Executor(entry.Arg0, entry.Arg1, serviceProvider, cancellationToken);
                }
            }
        }
        finally
        {
            this.count = 0;
            this.overflow?.Clear();
        }
    }

    /// <summary>
    /// Synchronously executes all queued commands using their sync executors.
    /// Throws <see cref="InvalidOperationException"/> if a command has no sync executor
    /// or if a producer is not configured with <see cref="ForgetStrategy.FireForget"/>.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider.</param>
    public void Execute(IServiceProvider serviceProvider)
    {
        try
        {
            int inlineCount = Math.Min(this.count, 4);
            for (int i = 0; i < inlineCount; i++)
            {
                var entry = this.buffer[i];
                if (entry.SyncExecutor is null)
                    throw new InvalidOperationException(
                        "No synchronous executor registered for this command type. Use SaveChangesAsync() instead.");
                entry.SyncExecutor(entry.Arg0, entry.Arg1, serviceProvider);
            }

            if (this.overflow is { Count: > 0 } overflow)
            {
                for (int i = 0; i < overflow.Count; i++)
                {
                    var entry = overflow[i];
                    if (entry.SyncExecutor is null)
                        throw new InvalidOperationException(
                            "No synchronous executor registered for this command type. Use SaveChangesAsync() instead.");
                    entry.SyncExecutor(entry.Arg0, entry.Arg1, serviceProvider);
                }
            }
        }
        finally
        {
            this.count = 0;
            this.overflow?.Clear();
        }
    }
}
