using Confluent.Kafka;
using Confluent.Kafka.Admin;
using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Extensions.MiddlewareBuilders;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
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
        /// <summary>
        /// Configures a topic for the specified message type.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="modelBuilder">The model builder instance.</param>
        /// <param name="topic">Action to configure the topic.</param>
        /// <returns>The model builder instance.</returns>
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

        /// <summary>
        /// Configures the topic to use a specific serialization strategy identified by options type.
        /// </summary>
        /// <typeparam name="TSerializer">The serializer type.</typeparam>
        /// <typeparam name="TOptions">The options type that identifies the serialization strategy.</typeparam>
        /// <param name="configure">Optional action to configure the strategy-specific settings.</param>
        /// <returns>The topic type builder instance.</returns>
        public TopicTypeBuilder<T> UseSerializer<TSerializer, TOptions>(Action<TOptions>? configure = null)
            where TOptions : class, new()
            where TSerializer : class, IMessageSerializer<T, TOptions>, IMessageDeserializer<T>, new()
        {
            var settings = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<SerializationMiddlewareSettings<T>>();

            TSerializer serializer = new();
            configure?.Invoke(serializer.Options);
            settings.Deserializer = serializer;
            settings.Serializer = serializer;

            return this;
        }

        /// <summary>
        /// Configures the topic to use System.Text.Json for serialization.
        /// This is a convenience method for the built-in JsonSerializerOptions strategy.
        /// </summary>
        /// <param name="configure">Action to configure System.Text.Json settings.</param>
        /// <returns>The topic type builder instance.</returns>
        public TopicTypeBuilder<T> UseSystemTextJson(Action<JsonSerializerOptions>? configure = null)
        {
            return UseSerializer<SystemTextJsonSerializer<T>, JsonSerializerOptions>(configure);
        }
    }
}

public class ProducerBuilder<T>(ModelBuilder modelBuilder)
    where T : class
{
    /// <summary>
    /// Configures the producer to use a specific property as message key when producing the message.
    /// </summary>
    /// <typeparam name="TProp"></typeparam>
    /// <param name="keyPropertyAccessor"></param>
    /// <remarks>
    /// Extremely important, as it allows the producer to correctly partition messages based on the key.
    /// </remarks>
    /// <returns></returns>
    public ProducerBuilder<T> HasKey<TProp>(Expression<Func<T, TProp>> keyPropertyAccessor)
    {
        var settings = ServiceProviderCache.Instance
            .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
            .GetRequiredService<ProducerMiddlewareSettings<T>>();

        settings.KeyPropertyAccessor = keyPropertyAccessor;

        return this;
    }

    /// <summary>
    /// Configures the producer to have no key (returns null for key), which means messages will be distributed randomly across partitions.
    /// </summary>
    /// <remarks>
    /// This disables automatic key discovery and explicitly sets the key to null for all messages.
    /// Use this when you want random partition distribution instead of key-based partitioning.
    /// </remarks>
    /// <returns></returns>
    public ProducerBuilder<T> HasNoKey()
    {
        var settings = ServiceProviderCache.Instance
            .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
            .GetRequiredService<ProducerMiddlewareSettings<T>>();

        settings.SetNoKey();

        return this;
    }

    /// <summary>
    /// Configures the outbox middleware for the producer.
    /// </summary>
    /// <param name="configure">Action to configure the outbox middleware settings.</param>
    /// <remarks>
    /// Automatically enables the batch middleware when outbox is configured.
    /// </remarks>
    /// <returns>The producer builder instance.</returns>
    public ProducerBuilder<T> HasOutbox(Action<OutboxBuilder<T>>? configure = null)
    {
        var outboxSettings = ServiceProviderCache.Instance
            .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
            .GetRequiredService<OutboxMiddlewareSettings<T>>();

        outboxSettings.EnableMiddleware();

        var builder = new OutboxBuilder<T>(outboxSettings);

        configure?.Invoke(builder);

        modelBuilder.Entity<OutboxMessage>(outbox =>
        {
            outbox.HasQueryFilter(outbox => !outbox.IsSuccessfullyProcessed);

            outbox.ToTable("OutboxMessages");

            outbox.HasKey(x => x.Id);

            outbox.Property(outbox => outbox.SequenceNumber)
                  .ValueGeneratedOnAdd()
                  .IsRequired();
        });

        if (outboxSettings.Strategy is OutboxPublishingStrategy.ImmediateWithFallback)
        {
            // Ensure that forget middleware is enabled when using ImmediateWithFallback strategy
            var forgetSettings = ServiceProviderCache.Instance
                .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
                .GetRequiredService<ProducerForgetMiddlewareSettings<T>>();

            forgetSettings.EnableMiddleware();
        }

        return this;
    }





    /// <summary>
    /// Configures the forget middleware for the producer.
    /// Supports both await-forget and fire-forget strategies.
    /// </summary>
    /// <param name="configure">Action to configure the forget middleware settings.</param>
    /// <returns>The producer builder instance.</returns>
    public ProducerBuilder<T> HasForget(Action<ProducerForgetBuilder<T>>? configure = null)
    {
        var settings = ServiceProviderCache.Instance
            .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
            .GetRequiredService<ProducerForgetMiddlewareSettings<T>>();

        // Enable the middleware by default when HasForget is called
        settings.IsMiddlewareEnabled = true;

        var builder = new ProducerForgetBuilder<T>(settings);
        configure?.Invoke(builder);
        return this;
    }


}

public class ConsumerBuilder<T>(ModelBuilder modelBuilder)
    where T : class
{
    /// <summary>
    /// Configures the inbox middleware for the consumer.
    /// </summary>
    /// <param name="configure">Action to configure the inbox middleware settings.</param>
    /// <returns>The consumer builder instance.</returns>
    public ConsumerBuilder<T> HasInbox(Action<InboxBuilder<T>>? configure = null)
    {
        //modelBuilder.Entity<InboxMessage>();

        var settings = ServiceProviderCache.Instance
            .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
            .GetRequiredService<InboxMiddlewareSettings<T>>();

        // Enable the middleware by default when HasInbox is called
        settings.IsMiddlewareEnabled = true;

        var builder = new InboxBuilder<T>(settings);
        configure?.Invoke(builder);
        return this;
    }

    /// <summary>
    /// Configures the forget middleware for the consumer.
    /// Supports both await-forget and fire-forget strategies.
    /// </summary>
    /// <param name="configure">Action to configure the forget middleware settings.</param>
    /// <returns>The consumer builder instance.</returns>
    public ConsumerBuilder<T> HasForget(Action<ConsumerForgetBuilder<T>>? configure = null)
    {
        var settings = ServiceProviderCache.Instance
            .GetOrAdd(KafkaOptionsExtension.CachedOptions!, true)
            .GetRequiredService<ConsumerForgetMiddlewareSettings<T>>();

        // Enable the middleware by default when HasForget is called
        settings.IsMiddlewareEnabled = true;

        var builder = new ConsumerForgetBuilder<T>(settings);
        configure?.Invoke(builder);
        return this;
    }
}
