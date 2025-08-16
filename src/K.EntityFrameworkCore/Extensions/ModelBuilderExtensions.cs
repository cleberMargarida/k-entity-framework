using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Extensions.MiddlewareBuilders;
using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Consumer;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
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
        public static ModelBuilder Topic<T>(this ModelBuilder modelBuilder, Action<TopicTypeBuilder<T>> topic) where T : class
        {
            topic(new TopicTypeBuilder<T>(modelBuilder));
            return modelBuilder;
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
        public ProducerBuilder<T> UseOutbox(Action<OutboxBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<OutboxMiddlewareOptions<T>>();

            var builder = new OutboxBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the retry middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the retry middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseRetry(Action<ProducerRetryBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerRetryMiddlewareOptions<T>>();

            var builder = new ProducerRetryBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the circuit breaker middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseCircuitBreaker(Action<ProducerCircuitBreakerBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerCircuitBreakerMiddlewareOptions<T>>();

            var builder = new ProducerCircuitBreakerBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the throttle middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the throttle middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseThrottle(Action<ProducerThrottleBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerThrottleMiddlewareOptions<T>>();

            var builder = new ProducerThrottleBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the fire-and-forget middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the fire-and-forget middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseFireForget(Action<ProducerFireForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerFireForgetMiddlewareOptions<T>>();

            var builder = new ProducerFireForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the await-and-forget middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the await-and-forget middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseAwaitForget(Action<ProducerAwaitForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerAwaitForgetMiddlewareOptions<T>>();

            var builder = new ProducerAwaitForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the batch middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the batch middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseBatch(Action<ProducerBatchBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerBatchMiddlewareOptions<T>>();

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
        public ConsumerBuilder<T> UseInbox(Action<InboxBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<InboxMiddlewareOptions<T>>();

            var builder = new InboxBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the retry middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the retry middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseRetry(Action<ConsumerRetryBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerRetryMiddlewareOptions<T>>();

            var builder = new ConsumerRetryBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the circuit breaker middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseCircuitBreaker(Action<ConsumerCircuitBreakerBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerCircuitBreakerMiddlewareOptions<T>>();

            var builder = new ConsumerCircuitBreakerBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the throttle middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the throttle middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseThrottle(Action<ConsumerThrottleBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerThrottleMiddlewareOptions<T>>();

            var builder = new ConsumerThrottleBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the batch middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the batch middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseBatch(Action<ConsumerBatchBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerBatchMiddlewareOptions<T>>();

            var builder = new ConsumerBatchBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the await-and-forget middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the await-and-forget middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseAwaitForget(Action<ConsumerAwaitForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerAwaitForgetMiddlewareOptions<T>>();

            var builder = new ConsumerAwaitForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the fire-and-forget middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the fire-and-forget middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseFireForget(Action<ConsumerFireForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ConsumerFireForgetMiddlewareOptions<T>>();

            var builder = new ConsumerFireForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }
    }
}
