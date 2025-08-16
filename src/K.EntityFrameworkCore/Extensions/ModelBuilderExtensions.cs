using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Extensions.MiddlewareBuilders;
using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Consumer;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]

namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ModelBuilder"/> to configure domain events.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        private static readonly ConcurrentDictionary<Type, bool> configuredTypes = new();

        /// <summary>
        /// Configures a topic for the specified message type. This method ensures that each message type
        /// is configured only once to avoid duplicate configuration.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="modelBuilder">The model builder instance.</param>
        /// <param name="topic">Action to configure the topic.</param>
        /// <returns>The model builder instance.</returns>
        public static ModelBuilder Topic<T>(this ModelBuilder modelBuilder, Action<TopicTypeBuilder<T>> topic) where T : class
        {
            if (!IsTypeConfigured<T>())
            {
                topic(new TopicTypeBuilder<T>(modelBuilder));
            }

            return modelBuilder;
        }

        private static bool IsTypeConfigured<T>() where T : class
        {
            return configuredTypes.TryAdd(typeof(T), true);
        }
    }

    public class TopicTypeBuilder<T>(ModelBuilder modelBuilder)
        where T : class
    {
        public TopicTypeBuilder<T> HasConsumer(Action<ConsumerBuilder<T>> consumer)
        {
            consumer(new ConsumerBuilder<T>(modelBuilder));
            return this;
        }

        public TopicTypeBuilder<T> HasName(string name)
        {
            return this;
        }

        public TopicTypeBuilder<T> HasProducer(Action<ProducerBuilder<T>> producer)
        {
            producer(new ProducerBuilder<T>(modelBuilder));
            return this;
        }

        public TopicTypeBuilder<T> HasSetting(Action<TopicSpecification> settings)
        {
            return this;
        }

        public TopicTypeBuilder<T> UseJsonSerializer(Action<JsonSerializerOptions> jsonSerializerOptions)
        {
            return this;
        }
    }

    public class ProducerBuilder<T>(ModelBuilder modelBuilder)
        where T : class
    {
        public ProducerBuilder<T> HasKey<TProp>(Expression<Func<T, TProp>> keyPropertyAccessor)
        {
            return this;
        }

        /// <summary>
        /// Configures the outbox middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the outbox middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasOutbox(Action<OutboxBuilder<T>>? configure = null)
        {
            modelBuilder.Entity<OutboxMessage>();

            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<OutboxMiddlewareOptions<T>>();

            // Enable the middleware by default when HasOutbox is called
            options.IsMiddlewareEnabled = true;

            var builder = new OutboxBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the retry middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the retry middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasRetry(Action<ProducerRetryBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerRetryMiddlewareOptions<T>>();

            // Enable the middleware by default when HasRetry is called
            options.IsMiddlewareEnabled = true;

            var builder = new ProducerRetryBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the circuit breaker middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasCircuitBreaker(Action<ProducerCircuitBreakerBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerCircuitBreakerMiddlewareOptions<T>>();

            // Enable the middleware by default when HasCircuitBreaker is called
            options.IsMiddlewareEnabled = true;

            var builder = new ProducerCircuitBreakerBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the throttle middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the throttle middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasThrottle(Action<ProducerThrottleBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerThrottleMiddlewareOptions<T>>();

            // Enable the middleware by default when HasThrottle is called
            options.IsMiddlewareEnabled = true;

            var builder = new ProducerThrottleBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the fire-and-forget middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the fire-and-forget middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasFireForget(Action<ProducerFireForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerFireForgetMiddlewareOptions<T>>();

            // Enable the middleware by default when HasFireForget is called
            options.IsMiddlewareEnabled = true;

            var builder = new ProducerFireForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the await-and-forget middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the await-and-forget middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasAwaitForget(Action<ProducerAwaitForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerAwaitForgetMiddlewareOptions<T>>();

            // Enable the middleware by default when HasAwaitForget is called
            options.IsMiddlewareEnabled = true;

            var builder = new ProducerAwaitForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the batch middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the batch middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> HasBatch(Action<ProducerBatchBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerBatchMiddlewareOptions<T>>();

            // Enable the middleware by default when HasBatch is called
            options.IsMiddlewareEnabled = true;

            var builder = new ProducerBatchBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }
    }

    public class ConsumerBuilder<T>(ModelBuilder modelBuilder)
        where T : class
    {
        public ConsumerBuilder<T> GroupId(string value)
        {
            return this;
        }

        /// <summary>
        /// Configures the inbox middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the inbox middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasInbox(Action<InboxBuilder<T>>? configure = null)
        {
            modelBuilder.Entity<InboxMessage>();

            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<InboxMiddlewareOptions<T>>();

            // Enable the middleware by default when HasInbox is called
            options.IsMiddlewareEnabled = true;

            var builder = new InboxBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the retry middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the retry middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasRetry(Action<ConsumerRetryBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerRetryMiddlewareOptions<T>>();

            // Enable the middleware by default when HasRetry is called
            options.IsMiddlewareEnabled = true;

            var builder = new ConsumerRetryBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the circuit breaker middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasCircuitBreaker(Action<ConsumerCircuitBreakerBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerCircuitBreakerMiddlewareOptions<T>>();

            // Enable the middleware by default when HasCircuitBreaker is called
            options.IsMiddlewareEnabled = true;

            var builder = new ConsumerCircuitBreakerBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the throttle middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the throttle middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasThrottle(Action<ConsumerThrottleBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerThrottleMiddlewareOptions<T>>();

            // Enable the middleware by default when HasThrottle is called
            options.IsMiddlewareEnabled = true;

            var builder = new ConsumerThrottleBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the batch middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the batch middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasBatch(Action<ConsumerBatchBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerBatchMiddlewareOptions<T>>();

            // Enable the middleware by default when HasBatch is called
            options.IsMiddlewareEnabled = true;

            var builder = new ConsumerBatchBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the await-and-forget middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the await-and-forget middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasAwaitForget(Action<ConsumerAwaitForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerAwaitForgetMiddlewareOptions<T>>();

            // Enable the middleware by default when HasAwaitForget is called
            options.IsMiddlewareEnabled = true;

            var builder = new ConsumerAwaitForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the fire-and-forget middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the fire-and-forget middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> HasFireForget(Action<ConsumerFireForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerFireForgetMiddlewareOptions<T>>();

            // Enable the middleware by default when HasFireForget is called
            options.IsMiddlewareEnabled = true;

            var builder = new ConsumerFireForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }
    }
}
