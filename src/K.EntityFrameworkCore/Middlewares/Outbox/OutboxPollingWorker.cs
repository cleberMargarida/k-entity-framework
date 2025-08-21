using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// Background worker that polls the outbox and publishes messages.
/// </summary>
/// <typeparam name="TDbContext">The EF Core DbContext type.</typeparam>
public sealed class OutboxPollingWorker<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private static ILogger<OutboxPollingWorker<TDbContext>> logger = default!;
    private readonly IServiceScope scope = default!;
    private readonly TDbContext context;
    private readonly OutboxPollingWorkerSettings<TDbContext> settings;
    private readonly IMiddlewareSpecifier<TDbContext>? middlewareSpecifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPollingWorker{TDbContext}"/> class.
    /// </summary>
    public OutboxPollingWorker(
          IServiceProvider applicationServiceProvider
        , IOptions<OutboxPollingWorkerSettings<TDbContext>> settings
        , IMiddlewareSpecifier<TDbContext>? middlewareSpecifier = null)
        : this(applicationServiceProvider.CreateScope(), settings)
    {
        logger = applicationServiceProvider.GetRequiredService<ILogger<OutboxPollingWorker<TDbContext>>>();
        this.middlewareSpecifier = middlewareSpecifier;
    }

    private OutboxPollingWorker(
          IServiceScope serviceScope
        , IOptions<OutboxPollingWorkerSettings<TDbContext>> settings)
        : this(serviceScope.ServiceProvider.GetRequiredService<TDbContext>(), settings)
    {
        this.scope = serviceScope;
    }

    private OutboxPollingWorker(
          TDbContext context
        , IOptions<OutboxPollingWorkerSettings<TDbContext>> settings)
    {
        this.context = context;
        this.settings = settings.Value;
    }

    /// <summary>
    /// Executes the worker loop. Queries are first scoped by the coordination strategy
    /// so only owned rows are retrieved from the database.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var outboxMessages = context.Set<OutboxMessage>();
        var dbContextServiceProvider = context.GetInfrastructure();
        var coordination = scope.ServiceProvider.GetRequiredService<IOutboxCoordinationStrategy<TDbContext>>();

        int maxMessagesPerPoll = settings.MaxMessagesPerPoll;
        using var timer = new PeriodicTimer(settings.PollingInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var query = coordination.ApplyScope(outboxMessages);
            var outboxMessageArray = await query.Take(maxMessagesPerPoll).ToArrayAsync(stoppingToken);
            var deferedExecutions = new Task[outboxMessageArray.Length];
            var outboxMessageTypes = new HashSet<Type?>();

            for (int i = 0; i < outboxMessageArray.Length; i++)
            {
                OutboxMessage? o = outboxMessageArray[i];
                deferedExecutions[i] = ExecuteOutboxMessageAsync(o, dbContextServiceProvider, stoppingToken);
                outboxMessageTypes.Add(o.TypeLoaded);
            }

            await Task.WhenAll(deferedExecutions);

            var topicFlushments = outboxMessageTypes
                .Select(type =>
                    Task.Factory.StartNew(
                        () => dbContextServiceProvider.GetRequiredKeyedService<IBatchProducer>(type).Flush(stoppingToken)
                        , stoppingToken
                        , TaskCreationOptions.LongRunning
                        , TaskScheduler.Default))
                .ToArray();

            await Task.WhenAll(topicFlushments);
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private Task ExecuteOutboxMessageAsync(OutboxMessage outboxMessage, IServiceProvider dbContextServiceProvider, CancellationToken stoppingToken)
    {
        ScopedCommand? command = CreateScopedCommandWithAutoGenerator(outboxMessage) ??
                                 CreateScopedCommandWithClr(outboxMessage);
        return command.Invoke(dbContextServiceProvider, stoppingToken).AsTask();
    }

    private ScopedCommand? CreateScopedCommandWithAutoGenerator(OutboxMessage outboxMessage)
    {
        return middlewareSpecifier?.DeferedExecution(outboxMessage);
    }

    private static ScopedCommand CreateScopedCommandWithClr(OutboxMessage outboxMessage)
    {
        return deferedExecutionMethodByType.GetOrAdd(outboxMessage.Type, LoadDeferedExecutionMethod).Invoke(outboxMessage);
    }

    private static readonly ConcurrentDictionary<string, Func<OutboxMessage, ScopedCommand>> deferedExecutionMethodByType = [];

    private static Func<OutboxMessage, ScopedCommand> LoadDeferedExecutionMethod(string assemblyQualifiedName)
    {
        logger.LogWarning("Source generator not found for type '{MessageType}'. Falling back to CLR reflection strategy. " +
                              "Consider adding package 'K.EntityFrameworkCore.CodeGen' for better performance.", outboxMessage.Type);

        var messageParam = Expression.Parameter(typeof(OutboxMessage), "outboxMessage");

        var method = typeof(OutboxPollingWorker<TDbContext>)
            .GetMethod(nameof(DeferedExecution), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(Type.GetType(assemblyQualifiedName, true)!);

        var body = Expression.Call(null, method, messageParam);

        return Expression.Lambda<Func<OutboxMessage, ScopedCommand>>(body, messageParam).Compile();
    }

    /// <summary>
    /// Processes an outbox message by invoking the producer middleware for deferred execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="outboxMessage"></param>
    /// <returns></returns>
    public static ScopedCommand DeferedExecution<T>(OutboxMessage outboxMessage)
        where T : class
    {
        return new MiddlewareInvokeCommand<T>(outboxMessage).ExecuteAsync;
    }

    readonly struct MiddlewareInvokeCommand<T>(OutboxMessage outboxMessage)
        where T : class
    {
        public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            outboxMessage.TypeLoaded = typeof(T);
            outboxMessage.ToEnvelope<T>();

            var producer = serviceProvider.GetRequiredKeyedService<IBatchProducer>(typeof(T));

            var options = serviceProvider.GetRequiredService<ProducerMiddlewareSettings<T>>();

            //TODO: avoid box -> unbox -> box -> unbox -> box
            producer.Produce(outboxMessage.AggregateId, new Message<string, byte[]>
            {
                //Headers = ,
                Key = outboxMessage.AggregateId!,
                Value = outboxMessage.Payload,

            }, HandleDeliveryReport);

            return ValueTask.CompletedTask;
        }

        private void HandleDeliveryReport(DeliveryReport<string, byte[]> report)
        {
            outboxMessage.ProcessedAt = report.Timestamp.UtcDateTime;
            bool processed = report.Error.Code is ErrorCode.NoError;
            if (processed)
            {
                outboxMessage.IsSuccessfullyProcessed = processed;
            }
            else
            {
                outboxMessage.Retries++;
            }
        }
    }

    /// <summary>
    /// Disposes the worker scope.
    /// </summary>
    public override void Dispose()
    {
        scope?.Dispose();
        base.Dispose();
    }
}
