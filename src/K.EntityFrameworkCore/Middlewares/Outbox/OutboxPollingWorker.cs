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
using System.Diagnostics;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// Background worker that polls the outbox and publishes messages.
/// </summary>
/// <typeparam name="TDbContext">The EF Core DbContext type.</typeparam>
public sealed class OutboxPollingWorker<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScope scope = default!;
    private readonly TDbContext context;
    private readonly OutboxPollingWorkerSettings<TDbContext> settings;
    private readonly ILogger<OutboxPollingWorker<TDbContext>> logger = default!;
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
        this.logger = applicationServiceProvider.GetRequiredService<ILogger<OutboxPollingWorker<TDbContext>>>();
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
        var coordination = scope.ServiceProvider.GetRequiredService<IOutboxCoordinationStrategy<TDbContext>>();
        var outboxMessages = context.Set<OutboxMessage>();

        int maxMessagesPerPoll = settings.MaxMessagesPerPoll;
        using var timer = new PeriodicTimer(settings.PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var query = coordination.ApplyScope(outboxMessages);

            var list = await query.Take(maxMessagesPerPoll).ToListAsync(stoppingToken);

            var deferedExecutions = list.Select(DeferedExecution);

            IServiceProvider serviceProvider = context.GetInfrastructure();

            var batchs = list
                .Select(o => o.Type)
                .ToHashSet()
                .Select(type =>
                    Task.Factory.StartNew(
                        () => serviceProvider.GetKeyedService<IBatchProducer>(type)!.Flush(stoppingToken)
                        , stoppingToken
                        , TaskCreationOptions.LongRunning
                        , TaskScheduler.Default));

            // confirm all defered executions
            await Task.WhenAll(deferedExecutions);

            // finished to produce on kafka
            await Task.WhenAll(batchs);

            // mark all messages as processed
            await context.SaveChangesAsync(stoppingToken);

            sw.Stop();
        }
    }

    private Task DeferedExecution(OutboxMessage outboxMessage)
    {
        return middlewareSpecifier?.DeferedExecution(outboxMessage).Invoke(
            context.GetInfrastructure()/*TODO share instance*/
            , CancellationToken.None).AsTask();
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
            Type type = typeof(T);

            outboxMessage.Type = type;

            var producer = serviceProvider.GetRequiredKeyedService<IBatchProducer>(type);

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
                outboxMessage.IsSucceeded = processed;
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
