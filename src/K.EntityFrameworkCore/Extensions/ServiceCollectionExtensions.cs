using K.EntityFrameworkCore.Middlewares.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add outbox Kafka worker services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a hosted service that polls the outbox table and publishes messages to Kafka.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the Entity Framework Core <see cref="DbContext"/> used for outbox storage.</typeparam>
    /// <param name="services">The service collection to add the worker to.</param>
    /// <param name="configureWorker">
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    public static IServiceCollection AddOutboxKafkaWorker<TDbContext>(
        this IServiceCollection services,
        Action<OutboxWorkerBuilder<TDbContext>>? configureWorker = null)
        where TDbContext : DbContext
    {
        var builder = new OutboxWorkerBuilder<TDbContext>(services);
        builder.UseSingleNode();
        configureWorker?.Invoke(builder);
        services.TryAddSingleton(builder);
        services.AddHostedService<OutboxPollingWorker<TDbContext>>();

        // reflection but only once to register the source generated code, avoiding more reflection
        var middlewareSpecifier = System.Reflection.Assembly.GetEntryAssembly()?
            .GetTypes()
            .SingleOrDefault(t => t.IsAssignableTo(typeof(IMiddlewareSpecifier<TDbContext>)));

        if (middlewareSpecifier != null)
        {
            services.TryAddSingleton(typeof(IMiddlewareSpecifier<TDbContext>), middlewareSpecifier);
        }

        return services;
    }
}

/// <summary>
/// Settings for the outbox polling worker that processes messages from the outbox table.
/// Provides configuration for polling behavior and coordination strategy.
/// </summary>
public class OutboxPollingWorkerSettings<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the interval in milliseconds for polling the outbox.
    /// </summary>
    /// <remarks>
    /// Default is <c>1000</c> milliseconds (1 second).
    /// </remarks>
    public int PollingIntervalMilliseconds
    {
        get => PollingInterval.Milliseconds;
        set => PollingInterval = TimeSpan.FromMilliseconds(value);
    }

    /// <summary>
    /// Gets or sets the interval for polling the outbox.
    /// </summary>
    /// <remarks>
    /// Default is <c>00:00:01</c> (1 second).
    /// </remarks>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum number of messages to process in a single poll.
    /// Default is <c>100</c>.
    /// </summary>
    public int MaxMessagesPerPoll { get; set; } = 100;
}
