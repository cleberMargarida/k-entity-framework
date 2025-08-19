using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.MiddlewareOptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace K.EntityFrameworkCore.Middlewares.Producer;

internal class OutboxMiddleware<T>(OutboxMiddlewareOptions<T> outbox, ICurrentDbContext dbContext) : Middleware<T>(outbox)
    where T : class
{
    private readonly DbContext context = dbContext.Context;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        DbSet<OutboxMessage> outboxMessages = context.Set<OutboxMessage>();

        OutboxMessage message = envelope.ToOutboxMessage();

        outboxMessages.Add(message);

        return outbox.Strategy switch
        {
            OutboxPublishingStrategy.ImmediateWithFallback => base.InvokeAsync(envelope, cancellationToken),
            OutboxPublishingStrategy.BackgroundOnly => ValueTask.CompletedTask,
            _ => throw new NotSupportedException($"The outbox strategy '{outbox.Strategy}' is not supported.")
        };
    }
}

/// <summary>
/// Defines the contract for scoping outbox queries to the subset of rows
/// this worker instance is allowed to process. Implementations must return
/// an expression that EF Core can translate to SQL so rows are filtered at the database.
/// </summary>
public interface IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Returns a SQL-translatable query filter that limits the outbox rows
    /// to those owned by this worker instance. This prevents loading and discarding rows.
    /// </summary>
    /// <param name="source">The base outbox query.</param>
    /// <returns>The filtered query that this worker should process.</returns>
    IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source);
}

/// <summary>
/// A no-op coordination strategy where a single worker processes all rows.
/// </summary>
internal sealed class SingleNodeCoordination<TDbContext> : IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        return source;
    }
}

/// <summary>
/// A no-op coordination strategy where a single worker processes all rows.
/// </summary>
internal sealed class ExclusiveNodeCoordination<TDbContext>(TDbContext context) : IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    private readonly IProducer<string, byte[]> producer = context.GetInfrastructure().GetRequiredService<IProducer<string, byte[]>>();

    /*
     string topic = $"__{AppDomain.CurrentDomain.FriendlyName.ToLower()}.{typeof(TDbContext).Name.ToLower()}.poll";
        Message<string, byte[]> emptyMessage = new();

        //periodically publish to force poll.
        while (false) { }
        //await producer.ProduceAsync(topic, emptyMessage, cancellationToken);
     */

    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        return source;
    }
}

/// <summary>
/// Kafka-backed sharding strategy that owns a subset of virtual buckets and filters
/// by <see cref="OutboxMessage.AggregateId"/> using a SQL-translatable <c>IN</c> predicate.
/// Bucket ownership is coordinated via Kafka heartbeats (implementation hidden).
/// </summary>
/// <remarks>
/// Initializes the strategy.
/// </remarks>
/// <param name="ownedBuckets">
/// The set of virtual buckets owned by this worker. How this set is computed
/// (heartbeats, partition assignment, etc.) is an internal detail.
/// </param>
internal sealed class KafkaBucketSharding<TDbContext>(int[] ownedBuckets) : IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        // EF translates Contains(array) on a property to SQL IN (...)
        //return source.Where(e => ownedBuckets.Contains(e.AggregateId));
        return source;
    }
}

/// <summary>
/// Builder used to configure outbox worker services with a fluent, high-level API.
/// </summary>
/// <typeparam name="TDbContext">The EF Core DbContext type.</typeparam>
public sealed class OutboxWorkerBuilder<TDbContext>
    where TDbContext : DbContext
{
    internal OutboxWorkerBuilder(IServiceCollection services) => Services = services;

    internal IServiceCollection Services { get; }

    /// <summary>
    /// Sets the interval in milliseconds for polling the outbox.
    /// </summary>
    /// <param name="intervalMilliseconds">Polling interval in milliseconds.</param>
    /// <remarks>
    /// Default is <c>1000</c> milliseconds (1 second).
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxWorkerBuilder<TDbContext> WithPollingInterval(int intervalMilliseconds)
    {
        Services.Configure<OutboxPollingWorkerSettings<TDbContext>>(setting => setting.PollingIntervalMilliseconds = intervalMilliseconds);
        return this;
    }

    /// <summary>
    /// Sets the interval for polling the outbox.
    /// </summary>
    /// <param name="interval">Polling interval.</param>
    /// <remarks>
    /// Default is <c>00:00:01</c> (1 second).
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxWorkerBuilder<TDbContext> WithPollingInterval(TimeSpan interval)
    {
        Services.Configure<OutboxPollingWorkerSettings<TDbContext>>(setting => setting.PollingInterval = interval);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of messages to process in a single poll.
    /// </summary>
    /// <param name="maxMessages">The maximum number of messages per poll.</param>
    /// <remarks>
    /// Default is <c>100</c>.
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxWorkerBuilder<TDbContext> WithMaxMessagesPerPoll(int maxMessages)
    {
        Services.Configure<OutboxPollingWorkerSettings<TDbContext>>(settings => settings.MaxMessagesPerPoll = maxMessages);
        return this;
    }

    /// <summary>
    /// Configures single-node coordination (no cluster at all).
    /// </summary>
    public OutboxWorkerBuilder<TDbContext> UseSingleNode()
    {
        Services.TryAddSingleton<IOutboxCoordinationStrategy<TDbContext>, SingleNodeCoordination<TDbContext>>();
        return this;
    }

    /// <summary>
    /// Configures exclusive-node coordination (clustered, but only one works at a time)
    /// </summary>
    public OutboxWorkerBuilder<TDbContext> UseExclusiveNode()
    {
        Services.TryAddSingleton<IOutboxCoordinationStrategy<TDbContext>, ExclusiveNodeCoordination<TDbContext>>();
        return this;
    }

    /// <summary>
    /// Configures Kafka-backed sharding using precomputed buckets on the outbox entity.
    /// Only the owned buckets will be queried from the database.
    /// </summary>
    /// <param name="virtualBucketCount">The total number of virtual buckets.</param>
    /// A delegate that resolves the set of owned buckets for this worker.
    /// The delegate can read environment, config, or Kafka assignment and returns the bucket ids.
    /// </param>
    public OutboxWorkerBuilder<TDbContext> WorkBalance(
        //int virtualBucketCount
        )
    {
        Services.TryAddSingleton<IOutboxCoordinationStrategy<TDbContext>>();
        return this;
    }
}

/// <summary>
/// Background worker that polls the outbox and publishes messages.
/// </summary>
/// <typeparam name="TDbContext">The EF Core DbContext type.</typeparam>
internal sealed class OutboxPollingWorker<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScope scope = default!;
    private readonly TDbContext context;
    private readonly OutboxPollingWorkerSettings<TDbContext> settings;

    public OutboxPollingWorker(IServiceProvider applicationServiceProvider, IOptions<OutboxPollingWorkerSettings<TDbContext>> settings) : this(applicationServiceProvider.CreateScope(), settings)
    {
    }

    private OutboxPollingWorker(IServiceScope serviceScope, IOptions<OutboxPollingWorkerSettings<TDbContext>> settings) : this(serviceScope.ServiceProvider.GetRequiredService<TDbContext>(), settings)
    {
        scope = serviceScope;
    }

    private OutboxPollingWorker(TDbContext context, IOptions<OutboxPollingWorkerSettings<TDbContext>> settings)
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
            var query = coordination.ApplyScope(outboxMessages);
            var batch = (await query.Take(maxMessagesPerPoll).ToListAsync(stoppingToken)).Select(DeferedExecution).ToArray();
            Task.WaitAll(batch, stoppingToken);
        }
    }

    private Task DeferedExecution(OutboxMessage outboxMessage)
    {
        return Task.CompletedTask;
        //return this.ProcessOutboxMessageAsync(outboxMessage);
    }

    internal Task DeferedExecution<T>(OutboxMessage outboxMessage)
        where T : class
    {
        IServiceProvider serviceProvider = context.GetInfrastructure();
        Envelope<T> envelope = outboxMessage.ToEnvelope<T>();

        return serviceProvider.GetRequiredService<OutboxProducerMiddlewareInvoker<T>>().InvokeAsync(envelope).AsTask();
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
