using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Extensions.MiddlewareBuilders;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]

namespace K.EntityFrameworkCore.Extensions;

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

    /// <summary>
    /// Configures worker-global outbox settings such as polling interval, batch size,
    /// and coordination strategy. These settings apply to the single background outbox
    /// worker and are shared across all message types.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <param name="configure">Action to configure global outbox worker settings via <see cref="OutboxGlobalBuilder"/>.</param>
    /// <returns>The model builder instance.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.HasOutboxWorker(worker => worker
    ///     .WithPollingInterval(TimeSpan.FromSeconds(5))
    ///     .WithMaxMessagesPerPoll(50));
    /// </code>
    /// </example>
    public static ModelBuilder HasOutboxWorker(this ModelBuilder modelBuilder, Action<OutboxGlobalBuilder> configure)
    {
        configure(new OutboxGlobalBuilder(modelBuilder.Model));
        return modelBuilder;
    }

    /// <summary>
    /// Configures both outbox and inbox tables for message processing.
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <returns></returns>
    [Obsolete("Not needed, this will happen when you configure outbox/inbox on specific topics via Topic<T>.")]
    public static ModelBuilder HasOutboxInboxTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<object>(topic => topic.HasProducer(producer => producer.HasOutbox()).HasConsumer(consumer => consumer.HasInbox()));
        return modelBuilder;
    }
}

/// <summary>
/// Provides a fluent API for configuring a topic for a specific message type.
/// </summary>
/// <typeparam name="T">The message type for this topic.</typeparam>
public class TopicTypeBuilder<T>
    where T : class
{
    private readonly ModelBuilder modelBuilder;

    /// <summary>
    /// Initialize instance.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    public TopicTypeBuilder(ModelBuilder modelBuilder)
    {
        this.modelBuilder = modelBuilder;
        string topicName = typeof(T).IsNested ? typeof(T).FullName!.Replace('+', '.') : typeof(T).FullName ?? typeof(T).Name;
        this.modelBuilder.Model.SetTopicName<T>(topicName);
        this.modelBuilder.Model.SetTopicSpecification<T>(new TopicSpecification { Name = topicName });
    }

    /// <summary>
    /// Configures a consumer for this topic.
    /// </summary>
    /// <param name="consumer">Action to configure the consumer.</param>
    /// <returns>The topic builder instance for method chaining.</returns>
    public TopicTypeBuilder<T> HasConsumer(Action<ConsumerBuilder<T>> consumer)
    {
        consumer(new ConsumerBuilder<T>(this.modelBuilder));
        return this;
    }

    /// <summary>
    /// Sets the name of this topic.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>The topic builder instance for method chaining.</returns>
    public TopicTypeBuilder<T> HasName(string name)
    {
        this.modelBuilder.Model.SetTopicName<T>(name);

        return this;
    }

    /// <summary>
    /// Configures a producer for this topic.
    /// </summary>
    /// <param name="producer">Action to configure the producer.</param>
    /// <returns>The topic builder instance for method chaining.</returns>
    public TopicTypeBuilder<T> HasProducer(Action<ProducerBuilder<T>> producer)
    {
        producer(new ProducerBuilder<T>(this.modelBuilder));
        return this;
    }

    /// <summary>
    /// Configures topic-specific settings.
    /// </summary>
    /// <param name="settings">Action to configure the topic settings.</param>
    /// <returns>The topic builder instance for method chaining.</returns>
    public TopicTypeBuilder<T> HasSetting(Action<TopicSpecification> settings)
    {
        var topicSpecification = new TopicSpecification();
        settings(topicSpecification);
        this.modelBuilder.Model.SetTopicSpecification<T>(topicSpecification);
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
        var serializer = new TSerializer();
        configure?.Invoke(serializer.Options);
        this.modelBuilder.Model.SetSerializer<T, TSerializer>(serializer);

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

/// <summary>
/// Provides a fluent API for configuring a producer for a specific message type.
/// </summary>
/// <typeparam name="T">The message type for this producer.</typeparam>
/// <param name="modelBuilder">The model builder instance.</param>
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
        modelBuilder.Model.SetKeyPropertyAccessor<T>(keyPropertyAccessor);

        return this;
    }

    /// <summary>
    /// Configures the producer to have no key (returns null for key), which means messages will be distributed randomly across partitions.
    /// </summary>
    /// <remarks>
    /// This disables automatic key discovery and explicitly sets the key to null for all messages.
    /// Use this when you want random partition distribution instead of key-based partitioning.
    /// </remarks>
    /// <returns>The producer builder instance for method chaining.</returns>
    public ProducerBuilder<T> HasNoKey()
    {
        modelBuilder.Model.SetNoKey<T>();

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
        modelBuilder.Model.SetOutboxEnabled<T>();

        // Still need to configure entity for OutboxMessage table
        modelBuilder.Entity<OutboxMessage>(outbox =>
        {
            outbox.HasQueryFilter(outbox => !outbox.IsSuccessfullyProcessed);

            outbox.ToTable("outbox_messages");

            outbox.HasKey(x => x.Id);

            outbox.Property(outbox => outbox.SequenceNumber)
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            outbox.HasIndex(x => x.SequenceNumber);

            outbox.Property(outbox => outbox.Topic)
                  .HasMaxLength(255)
                  .IsRequired();

            var headersProperty = outbox.Property(outbox => outbox.Headers)
                  .HasConversion(
                      headers => JsonSerializer.Serialize(headers, default(JsonSerializerOptions)),
                      json => JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(json, default(JsonSerializerOptions))!);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assemblies.Any(a => a.GetName().Name?.StartsWith("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase) == true))
                headersProperty.HasColumnType("jsonb");
            else if (assemblies.Any(a => a.GetName().Name?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true))
                headersProperty.HasColumnType("nvarchar(max)");
            else if (assemblies.Any(a => a.GetName().Name?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true))
                headersProperty.HasColumnType("TEXT");

        });

        configure?.Invoke(new OutboxBuilder<T>(modelBuilder.Model));

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
        modelBuilder.Model.SetProducerForgetEnabled<T>();

        var settings = new ProducerForgetMiddlewareSettings<T>();
        configure?.Invoke(new ProducerForgetBuilder<T>(settings));
        modelBuilder.Model.SetProducerForgetStrategy<T>(settings.Strategy);

        return this;
    }

    /// <summary>
    /// Configures a header to be included in produced messages using the property name as the header key.
    /// </summary>
    /// <param name="headerValueAccessor">Expression to extract the header value from the message.</param>
    /// <returns>The producer builder instance for method chaining.</returns>
    public ProducerBuilder<T> HasHeader(Expression<Func<T, object>> headerValueAccessor)
    {
        string headerKey = GetPropertyName(headerValueAccessor);

        modelBuilder.Model.AddHeaderAccessor<T>(headerKey, headerValueAccessor);

        return this;
    }

    /// <summary>
    /// Configures a header to be included in produced messages with a custom header key.
    /// </summary>
    /// <param name="headerKey">The header key to use in the message.</param>
    /// <param name="headerValueAccessor">Expression to extract the header value from the message.</param>
    /// <returns>The producer builder instance for method chaining.</returns>
    public ProducerBuilder<T> HasHeader(string headerKey, Expression<Func<T, object>> headerValueAccessor)
    {
        modelBuilder.Model.AddHeaderAccessor<T>(headerKey, headerValueAccessor);

        return this;
    }

    private static string GetPropertyName<TSource>(Expression<Func<TSource, object>> expression)
    {
        Expression body = expression.Body;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        if (body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property)
        {
            return property.Name;
        }

        throw new ArgumentException($"Expression '{expression}' does not refer to a property.", nameof(expression));
    }
}

/// <summary>
/// Provides a fluent API for configuring a consumer for a specific message type.
/// </summary>
/// <typeparam name="T">The message type for this consumer.</typeparam>
/// <param name="modelBuilder">The model builder instance.</param>
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
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(entity => entity.HashId);
            entity.Property(e => e.HashId).ValueGeneratedNever();
            entity.HasIndex(entity => entity.ExpireAt);
            entity.HasQueryFilter(entity => entity.ExpireAt > DateTime.UtcNow);
        });

        modelBuilder.Model.SetInboxEnabled<T>();

        // Configure using the inbox builder
        configure?.Invoke(new InboxBuilder<T>(modelBuilder));

        return this;
    }

    /// <summary>
    /// Sets the maximum number of messages that can be buffered in memory for this message type.
    /// Default is inherited from global consumer configuration.
    /// Higher values provide better throughput but use more memory.
    /// Lower values reduce memory usage but may limit processing throughput.
    /// </summary>
    /// <param name="maxMessages">The maximum number of buffered messages.</param>
    /// <returns>The consumer builder instance.</returns>
    public ConsumerBuilder<T> HasMaxBufferedMessages(int maxMessages)
    {
        modelBuilder.Model.SetMaxBufferedMessages<T>(maxMessages);
        return this;
    }

    /// <summary>
    /// Sets the behavior when the message buffer reaches capacity for this message type.
    /// Default is inherited from global consumer configuration.
    /// </summary>
    /// <param name="mode">The backpressure mode to use.</param>
    /// <returns>The consumer builder instance.</returns>
    public ConsumerBuilder<T> HasBackpressureMode(ConsumerBackpressureMode mode)
    {
        modelBuilder.Model.SetBackpressureMode<T>(mode);
        return this;
    }

    /// <summary>
    /// Configures this message type to use a dedicated consumer connection.
    /// When enabled, a separate KafkaConsumerPollService will be created specifically for this type,
    /// allowing for type-specific consumer configurations and isolation.
    /// </summary>
    /// <param name="connection">Optional action to configure the dedicated connection settings.</param>
    /// <returns>The consumer builder instance.</returns>
    public ConsumerBuilder<T> HasExclusiveConnection(Action<IConsumerConfig>? connection = null)
    {
        modelBuilder.Model.SetExclusiveConnection<T>();
        modelBuilder.Model.SetExclusiveConnectionConfig<T>(connection);

        return this;
    }

    /// <summary>
    /// Configures a header filter that will run after deserialization to filter messages based on header values.
    /// Messages that don't match the filter criteria will be discarded and not processed further.
    /// </summary>
    /// <param name="headerKey">The header key to filter on.</param>
    /// <param name="expectedValue">The expected header value for the message to be processed.</param>
    /// <returns>The consumer builder instance.</returns>
    /// <example>
    /// <code>
    /// // Filter messages by tenant ID from header
    /// consumer.HasHeaderFilter("tenant-id", "tenant-123");
    /// 
    /// // Filter messages by region
    /// consumer.HasHeaderFilter("region", "US");
    /// </code>
    /// </example>
    public ConsumerBuilder<T> HasHeaderFilter(string headerKey, string expectedValue)
    {
        modelBuilder.Model.AddHeaderFilter<T>(headerKey, expectedValue);

        return this;
    }
}
