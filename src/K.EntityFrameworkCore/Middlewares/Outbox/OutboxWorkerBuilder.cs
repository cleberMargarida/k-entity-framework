using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

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
    /// <br></br>
    /// <br></br>
    /// Make sure this value is not lower than the most lower batch timeout.
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
    /// using default <see cref="ExclusiveNodeOptions"/>.
    /// </summary>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxWorkerBuilder<TDbContext> UseExclusiveNode()
        => UseExclusiveNode(configure: null);

    /// <summary>
    /// Configures exclusive-node coordination (clustered, but only one works at a time)
    /// with custom options. The leader is elected via a Kafka consumer group on a
    /// single-partition coordination topic; only the leader processes outbox rows.
    /// </summary>
    /// <param name="configure">
    /// Optional action to configure <see cref="ExclusiveNodeOptions"/>.
    /// When <c>null</c>, default option values are used.
    /// </param>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxWorkerBuilder<TDbContext> UseExclusiveNode(Action<ExclusiveNodeOptions>? configure)
    {
        if (configure is not null)
        {
            Services.Configure(configure);
        }

        // Remove any previously registered coordination strategy (e.g., the SingleNode default)
        // so the exclusive-node strategy takes precedence.
        Services.RemoveAll<IOutboxCoordinationStrategy<TDbContext>>();

        // Register the implementation as a singleton shared across both interfaces.
        Services.TryAddSingleton<ExclusiveNodeCoordination<TDbContext>>();

        Services.AddSingleton<IOutboxCoordinationStrategy<TDbContext>>(sp =>
            sp.GetRequiredService<ExclusiveNodeCoordination<TDbContext>>());

        Services.AddSingleton<IHostedService>(sp =>
            sp.GetRequiredService<ExclusiveNodeCoordination<TDbContext>>());

        return this;
    }

    /// <summary>
    /// Configures Kafka-backed sharding using precomputed buckets on the outbox entity.
    /// Only the owned buckets will be queried from the database.
    /// </summary>
    public OutboxWorkerBuilder<TDbContext> WorkBalance(
        //int virtualBucketCount
        )
    {
        Services.TryAddSingleton<IOutboxCoordinationStrategy<TDbContext>>();
        return this;
    }
}
