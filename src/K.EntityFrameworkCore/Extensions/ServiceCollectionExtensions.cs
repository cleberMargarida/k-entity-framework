using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace K.EntityFrameworkCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a hosted service that polls the outbox table and publishes messages to Kafka.
        /// </summary>
        /// <typeparam name="DbContext">The type of the Entity Framework Core <see cref="Microsoft.EntityFrameworkCore.DbContext"/> used for outbox storage.</typeparam>
        /// <param name="services">The service collection to add the worker to.</param>
        /// <param name="configureWorker">
        /// </param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public static IServiceCollection AddOutboxKafkaWorker<DbContext>(
            this IServiceCollection services,
            Action<OutboxWorkerBuilder<DbContext>>? configureWorker = null)
            where DbContext : Microsoft.EntityFrameworkCore.DbContext
        {
            var builder = new OutboxWorkerBuilder<DbContext>(services);
            configureWorker?.Invoke(builder);
            services.TryAddSingleton(builder);
            services.AddHostedService<OutboxPollingWorker<DbContext>>();
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
}
