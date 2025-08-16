using Confluent.Kafka;
using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Extensions.MiddlewareBuilders;
using K.EntityFrameworkCore.MiddlewareOptions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

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
            return this;
        }

        public TopicTypeBuilder<T> HasName(string name)
        {
            return this;
        }

        public TopicTypeBuilder<T> HasProducer(Action<ProducerBuilder<T>> producer)
        {
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

    public class ProducerBuilder<T>
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
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new OutboxMiddlewareOptions<T>());
            var builder = new OutboxBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the retry middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the retry middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseRetry(Action<RetryBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new RetryMiddlewareOptions<T>());
            var builder = new RetryBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the circuit breaker middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseCircuitBreaker(Action<CircuitBreakerBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new CircuitBreakerMiddlewareOptions<T>());
            var builder = new CircuitBreakerBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the throttle middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the throttle middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseThrottle(Action<ThrottleBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new ThrottleMiddlewareOptions<T>());
            var builder = new ThrottleBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the fire-and-forget middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the fire-and-forget middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseFireForget(Action<FireForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new FireForgetMiddlewareOptions<T>());
            var builder = new FireForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the await-and-forget middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the await-and-forget middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseAwaitForget(Action<AwaitForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new AwaitForgetMiddlewareOptions<T>());
            var builder = new AwaitForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the batch middleware for the producer.
        /// </summary>
        /// <param name="configure">Action to configure the batch middleware options.</param>
        /// <returns>The producer builder instance.</returns>
        public ProducerBuilder<T> UseBatch(Action<BatchBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new BatchMiddlewareOptions<T>());
            var builder = new BatchBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }
    }

    public class ConsumerBuilder<T>
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
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new InboxMiddlewareOptions<T>());
            var builder = new InboxBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the retry middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the retry middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseRetry(Action<RetryBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new RetryMiddlewareOptions<T>());
            var builder = new RetryBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the circuit breaker middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseCircuitBreaker(Action<CircuitBreakerBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new CircuitBreakerMiddlewareOptions<T>());
            var builder = new CircuitBreakerBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the throttle middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the throttle middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseThrottle(Action<ThrottleBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new ThrottleMiddlewareOptions<T>());
            var builder = new ThrottleBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the batch middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the batch middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseBatch(Action<BatchBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new BatchMiddlewareOptions<T>());
            var builder = new BatchBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the await-and-forget middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the await-and-forget middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseAwaitForget(Action<AwaitForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new AwaitForgetMiddlewareOptions<T>());
            var builder = new AwaitForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }

        /// <summary>
        /// Configures the fire-and-forget middleware for the consumer.
        /// </summary>
        /// <param name="configure">Action to configure the fire-and-forget middleware options.</param>
        /// <returns>The consumer builder instance.</returns>
        public ConsumerBuilder<T> UseFireForget(Action<FireForgetBuilder<T>>? configure = null)
        {
            var options = ServiceProviderCache.Instance.GetOrAdd(() => new FireForgetMiddlewareOptions<T>());
            var builder = new FireForgetBuilder<T>(options);
            configure?.Invoke(builder);
            return this;
        }
    }
}
