using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Helper methods for working with middleware settings stored as model annotations.
/// </summary>
internal static class ModelAnnotationHelpers
{
    /// <summary>
    /// Gets header property accessors for a producer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>Dictionary mapping header keys to property accessor expressions, or empty dictionary if not set.</returns>
    public static Dictionary<string, Expression> GetHeaderAccessors<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerHeaderAccessors(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as Dictionary<string, Expression> ?? [];
    }

    /// <summary>
    /// Sets header filter configuration for a consumer message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The mutable Entity Framework model.</param>
    /// <param name="headerKey">The header key to filter on.</param>
    /// <param name="expectedValue">The expected header value.</param>
    public static void AddHeaderFilter<T>(this IMutableModel model, string headerKey, string expectedValue)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerHeaderFilters(typeof(T));
        var existingFilters = model.FindAnnotation(annotationKey)?.Value as Dictionary<string, string> ?? [];
        existingFilters[headerKey] = expectedValue;
        model.SetAnnotation(annotationKey, existingFilters);
    }

    /// <summary>
    /// Gets header filter configurations for a consumer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>Dictionary mapping header keys to expected values, or empty dictionary if not set.</returns>
    public static Dictionary<string, string> GetHeaderFilters<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerHeaderFilters(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as Dictionary<string, string> ?? [];
    }

    /// <summary>
    /// Checks if header filters are enabled for a message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>True if header filters are configured, false otherwise.</returns>
    public static bool IsHeaderFilterEnabled<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerHeaderFilters(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value is Dictionary<string, string> filters && filters.Count > 0;
    }

    /// <summary>
    /// Sets the topic name for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="topicName">The topic name to set.</param>
    public static void SetTopicName<T>(this IMutableModel model, string topicName)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.TopicName(typeof(T));
        model.SetAnnotation(annotationKey, topicName);
    }

    /// <summary>
    /// Gets the topic name for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The topic name, or null if not set.</returns>
    public static string? GetTopicName<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.TopicName(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as string;
    }

    /// <summary>
    /// Sets the key property accessor for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="keyPropertyAccessor">The expression that defines how to extract the key from the message.</param>
    public static void SetKeyPropertyAccessor<T>(this IMutableModel model, Expression keyPropertyAccessor)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerKeyPropertyAccessor(typeof(T));
        model.SetAnnotation(annotationKey, keyPropertyAccessor);
    }

    /// <summary>
    /// Gets the key property accessor for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The key property accessor expression, or null if not set.</returns>
    public static Expression? GetKeyPropertyAccessor<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerKeyPropertyAccessor(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as Expression;
    }

    /// <summary>
    /// Sets that a message type should have no key (null key) in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    public static void SetNoKey<T>(this IMutableModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerHasNoKey(typeof(T));
        model.SetAnnotation(annotationKey, true);

        // Remove any existing key property accessor when setting no key
        var keyAccessorKey = ModelAnnotationKeys.ProducerKeyPropertyAccessor(typeof(T));
        model.RemoveAnnotation(keyAccessorKey);
    }

    /// <summary>
    /// Checks if a message type is configured to have no key.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>True if the message type is configured to have no key, false otherwise.</returns>
    public static bool HasNoKey<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerHasNoKey(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as bool? ?? false;
    }

    /// <summary>
    /// Sets the outbox middleware enabled state for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="enabled">Whether the outbox middleware is enabled.</param>
    public static void SetOutboxEnabled<T>(this IMutableModel model, bool enabled = true)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.OutboxEnabled(typeof(T));
        model.SetAnnotation(annotationKey, enabled);
    }

    /// <summary>
    /// Checks if the outbox middleware is enabled for a message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>True if the outbox middleware is enabled, false otherwise.</returns>
    public static bool IsOutboxEnabled<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.OutboxEnabled(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as bool? ?? false;
    }

    /// <summary>
    /// Sets the producing strategy for an outbox message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="strategy">The outbox producing strategy.</param>
    public static void SetOutboxPublishingStrategy<T>(this IMutableModel model, OutboxPublishingStrategy strategy)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.OutboxPublishingStrategy(typeof(T));
        model.SetAnnotation(annotationKey, strategy);
    }

    /// <summary>
    /// Gets the producing strategy for an outbox message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The outbox producing strategy, or null if not set.</returns>
    public static OutboxPublishingStrategy? GetOutboxPublishingStrategy<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.OutboxPublishingStrategy(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as OutboxPublishingStrategy?;
    }

    /// <summary>
    /// Sets the inbox middleware enabled state for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="enabled">Whether the inbox middleware is enabled.</param>
    public static void SetInboxEnabled<T>(this IMutableModel model, bool enabled = true)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxEnabled(typeof(T));
        model.SetAnnotation(annotationKey, enabled);
    }

    /// <summary>
    /// Checks if the inbox middleware is enabled for a message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>True if the inbox middleware is enabled, false otherwise.</returns>
    public static bool IsInboxEnabled<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxEnabled(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as bool? ?? false;
    }

    /// <summary>
    /// Sets the deduplication value accessor for an inbox message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="valueAccessor">The expression that defines how to extract values for deduplication.</param>
    public static void SetInboxDeduplicationValueAccessor<T>(this IMutableModel model, Expression valueAccessor)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxDeduplicationValueAccessor(typeof(T));
        model.SetAnnotation(annotationKey, valueAccessor);
    }

    /// <summary>
    /// Gets the deduplication value accessor for an inbox message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The deduplication value accessor expression, or null if not set.</returns>
    public static Expression? GetInboxDeduplicationValueAccessor<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxDeduplicationValueAccessor(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as Expression;
    }

    /// <summary>
    /// Sets the deduplication time window for an inbox message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="timeWindow">The deduplication time window.</param>
    public static void SetInboxDeduplicationTimeWindow<T>(this IMutableModel model, TimeSpan timeWindow)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxDeduplicationTimeWindow(typeof(T));
        model.SetAnnotation(annotationKey, timeWindow);
    }

    /// <summary>
    /// Gets the deduplication time window for an inbox message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The deduplication time window, or null if not set.</returns>
    public static TimeSpan? GetInboxDeduplicationTimeWindow<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxDeduplicationTimeWindow(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as TimeSpan?;
    }

    // Inbox cleanup interval helpers removed — cleanup scheduling is no longer part of the library.

    /// <summary>
    /// Sets the serializer type for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <typeparam name="TSerializer">The serializer type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="serializer">The serializer instance to set.</param>
    public static void SetSerializer<T, TSerializer>(this IMutableModel model, TSerializer serializer)
        where T : class
        where TSerializer : class
    {
        var annotationKey = ModelAnnotationKeys.SerializerInstance(typeof(T));
        model.SetAnnotation(annotationKey, serializer);
    }

    /// <summary>
    /// Gets the serializer for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The serializer type, or null if not set.</returns>
    public static object? GetSerializer<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.SerializerInstance(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value;
    }

    /// <summary>
    /// Sets the producer forget middleware enabled state for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="enabled">Whether the producer forget middleware is enabled.</param>
    public static void SetProducerForgetEnabled<T>(this IMutableModel model, bool enabled = true)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerForgetEnabled(typeof(T));
        model.SetAnnotation(annotationKey, enabled);
    }

    /// <summary>
    /// Checks if the producer forget middleware is enabled for a message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>True if the producer forget middleware is enabled, false otherwise.</returns>
    public static bool IsProducerForgetEnabled<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerForgetEnabled(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as bool? ?? false;
    }

    /// <summary>
    /// Sets the forget strategy for a producer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="strategy">The forget strategy.</param>
    public static void SetProducerForgetStrategy<T>(this IMutableModel model, ForgetStrategy strategy)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerForgetStrategy(typeof(T));
        model.SetAnnotation(annotationKey, strategy);
    }

    /// <summary>
    /// Gets the forget strategy for a producer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The forget strategy, or null if not set.</returns>
    public static ForgetStrategy? GetProducerForgetStrategy<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerForgetStrategy(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as ForgetStrategy?;
    }

    /// <summary>
    /// Sets the forget timeout for a producer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="timeout">The timeout duration.</param>
    public static void SetProducerForgetTimeout<T>(this IMutableModel model, TimeSpan timeout)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerForgetTimeout(typeof(T));
        model.SetAnnotation(annotationKey, timeout);
    }

    /// <summary>
    /// Gets the forget timeout for a producer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The forget timeout, or null if not set.</returns>
    public static TimeSpan? GetProducerForgetTimeout<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerForgetTimeout(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as TimeSpan?;
    }

    /// <summary>
    /// Sets the maximum buffered messages for a consumer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="maxMessages">The maximum number of buffered messages.</param>
    public static void SetMaxBufferedMessages<T>(this IMutableModel model, int maxMessages)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerMaxBufferedMessages(typeof(T));
        model.SetAnnotation(annotationKey, maxMessages);
    }

    /// <summary>
    /// Gets the maximum buffered messages for a consumer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The maximum buffered messages, or null if not set.</returns>
    public static int? GetMaxBufferedMessages<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerMaxBufferedMessages(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as int?;
    }

    /// <summary>
    /// Sets the backpressure mode for a consumer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="mode">The backpressure mode.</param>
    public static void SetBackpressureMode<T>(this IMutableModel model, ConsumerBackpressureMode mode)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerBackpressureMode(typeof(T));
        model.SetAnnotation(annotationKey, mode);
    }

    /// <summary>
    /// Gets the backpressure mode for a consumer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The backpressure mode, or null if not set.</returns>
    public static ConsumerBackpressureMode? GetBackpressureMode<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerBackpressureMode(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as ConsumerBackpressureMode?;
    }

    /// <summary>
    /// Sets the high water mark ratio for a consumer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="ratio">The high water mark ratio (0.0–1.0).</param>
    public static void SetHighWaterMarkRatio<T>(this IMutableModel model, double ratio)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerHighWaterMarkRatio(typeof(T));
        model.SetAnnotation(annotationKey, ratio);
    }

    /// <summary>
    /// Gets the high water mark ratio for a consumer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The high water mark ratio, or null if not set.</returns>
    public static double? GetHighWaterMarkRatio<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerHighWaterMarkRatio(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as double?;
    }

    /// <summary>
    /// Sets the low water mark ratio for a consumer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="ratio">The low water mark ratio (0.0–1.0).</param>
    public static void SetLowWaterMarkRatio<T>(this IMutableModel model, double ratio)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerLowWaterMarkRatio(typeof(T));
        model.SetAnnotation(annotationKey, ratio);
    }

    /// <summary>
    /// Gets the low water mark ratio for a consumer message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The low water mark ratio, or null if not set.</returns>
    public static double? GetLowWaterMarkRatio<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerLowWaterMarkRatio(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as double?;
    }

    /// <summary>
    /// Sets whether a consumer message type uses exclusive connection in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="exclusive">Whether the consumer uses exclusive connection.</param>
    public static void SetExclusiveConnection<T>(this IMutableModel model, bool exclusive = true)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerExclusiveConnection(typeof(T));
        model.SetAnnotation(annotationKey, exclusive);
    }

    /// <summary>
    /// Checks if a consumer message type uses exclusive connection.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>True if the consumer uses exclusive connection, false otherwise.</returns>
    public static bool HasExclusiveConnection<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerExclusiveConnection(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as bool? ?? false;
    }

    /// <summary>
    /// Checks if a consumer message type uses exclusive connection.
    /// </summary>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="type">The message type.</param>
    /// <returns>True if the consumer uses exclusive connection, false otherwise.</returns>
    public static bool HasExclusiveConnection(this IModel model, Type type)
    {
        var annotationKey = ModelAnnotationKeys.ConsumerExclusiveConnection(type);
        return model.FindAnnotation(annotationKey)?.Value as bool? ?? false;
    }

    /// <summary>
    /// Sets the exclusive connection configuration for a consumer message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The mutable Entity Framework model.</param>
    /// <param name="connectionConfig">The action to configure the consumer connection.</param>
    public static void SetExclusiveConnectionConfig<T>(this IMutableModel model, Action<IConsumerConfig>? connectionConfig)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerExclusiveConnectionConfig(typeof(T));
        model.SetAnnotation(annotationKey, connectionConfig);
    }

    /// <summary>
    /// Gets the exclusive connection configuration for a consumer message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The action to configure the consumer connection, or null if not set.</returns>
    public static Action<IConsumerConfig>? GetExclusiveConnectionConfig<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerExclusiveConnectionConfig(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as Action<IConsumerConfig>;
    }

    /// <summary>
    /// Sets header property accessors for a producer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="headerAccessors">Dictionary mapping header keys to property accessor expressions.</param>
    public static void SetHeaderAccessors<T>(this IMutableModel model, Dictionary<string, Expression> headerAccessors)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerHeaderAccessors(typeof(T));
        model.SetAnnotation(annotationKey, headerAccessors);
    }

    /// <summary>
    /// Adds a header property accessor for a producer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="headerKey">The header key.</param>
    /// <param name="headerValueAccessor">The expression that defines how to extract the header value from the message.</param>
    public static void AddHeaderAccessor<T>(this IMutableModel model, string headerKey, Expression headerValueAccessor)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ProducerHeaderAccessors(typeof(T));
        var existingAccessors = model.FindAnnotation(annotationKey)?.Value as Dictionary<string, Expression> ?? [];
        existingAccessors[headerKey] = headerValueAccessor;
        model.SetAnnotation(annotationKey, existingAccessors);
    }

    /// <summary>
    /// Sets the topic specification for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="topicSpecification">The topic specification to set.</param>
    public static void SetTopicSpecification<T>(this IMutableModel model, TopicSpecification topicSpecification)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.TopicSpecification(typeof(T));
        model.SetAnnotation(annotationKey, topicSpecification);
    }

    /// <summary>
    /// Gets the topic specification for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The topic specification, or null if not set.</returns>
    public static TopicSpecification? GetTopicSpecification<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.TopicSpecification(typeof(T));
        var topicSpecification = model.FindAnnotation(annotationKey)?.Value as TopicSpecification;
        return topicSpecification;
    }

    /// <summary>
    /// Sets the circuit breaker configuration for a consumer message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The mutable Entity Framework model.</param>
    /// <param name="config">The circuit breaker configuration.</param>
    public static void SetCircuitBreakerConfig<T>(this IMutableModel model, ICircuitBreakerConfig config)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerCircuitBreakerConfig(typeof(T));
        model.SetAnnotation(annotationKey, config);
    }

    /// <summary>
    /// Gets the circuit breaker configuration for a consumer message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The circuit breaker configuration, or null if not set.</returns>
    public static ICircuitBreakerConfig? GetCircuitBreakerConfig<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerCircuitBreakerConfig(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as ICircuitBreakerConfig;
    }

    /// <summary>
    /// Gets the circuit breaker configuration for a consumer message type.
    /// </summary>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="type">The message type.</param>
    /// <returns>The circuit breaker configuration, or null if not set.</returns>
    public static ICircuitBreakerConfig? GetCircuitBreakerConfig(this IModel model, Type type)
    {
        var annotationKey = ModelAnnotationKeys.ConsumerCircuitBreakerConfig(type);
        return model.FindAnnotation(annotationKey)?.Value as ICircuitBreakerConfig;
    }

    /// <summary>
    /// Adds a user-registered producer middleware type for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The mutable Entity Framework model.</param>
    /// <param name="middlewareType">The user middleware type to register.</param>
    public static void AddUserProducerMiddleware<T>(this IMutableModel model, Type middlewareType)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.UserProducerMiddlewares(typeof(T));
        var existing = model.FindAnnotation(annotationKey)?.Value as List<UserMiddlewareRegistration> ?? [];
        existing.Add(new UserMiddlewareRegistration(middlewareType));
        model.SetAnnotation(annotationKey, existing);
    }

    /// <summary>
    /// Gets the user-registered producer middleware types for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>List of user middleware registrations, or empty list if none registered.</returns>
    public static List<UserMiddlewareRegistration> GetUserProducerMiddlewares<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.UserProducerMiddlewares(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as List<UserMiddlewareRegistration> ?? [];
    }

    /// <summary>
    /// Adds a user-registered consumer middleware type for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The mutable Entity Framework model.</param>
    /// <param name="middlewareType">The user middleware type to register.</param>
    public static void AddUserConsumerMiddleware<T>(this IMutableModel model, Type middlewareType)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.UserConsumerMiddlewares(typeof(T));
        var existing = model.FindAnnotation(annotationKey)?.Value as List<UserMiddlewareRegistration> ?? [];
        existing.Add(new UserMiddlewareRegistration(middlewareType));
        model.SetAnnotation(annotationKey, existing);
    }

    /// <summary>
    /// Gets the user-registered consumer middleware types for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>List of user middleware registrations, or empty list if none registered.</returns>
    public static List<UserMiddlewareRegistration> GetUserConsumerMiddlewares<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.UserConsumerMiddlewares(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as List<UserMiddlewareRegistration> ?? [];
    }

    /// <summary>
    /// Gets all topic specifications stored in the model with their corresponding message types.
    /// </summary>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>Dictionary mapping message types to their topic specifications.</returns>
    public static Dictionary<Type, TopicSpecification> GetAllTopicSpecifications(this IModel model)
    {
        var result = new Dictionary<Type, TopicSpecification>();
        var prefix = ModelAnnotationKeys.TopicSpecification(typeof(object)).Replace("[System.Object]", "");

        foreach (var annotation in model.GetAnnotations())
        {
            if (!annotation.Name.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (annotation.Value is not TopicSpecification specification)
            {
                continue;
            }

            var typeName = annotation.Name[prefix.Length..].Trim('[', ']');

            var messageType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == typeName);

            if (messageType != null)
            {
                result[messageType] = specification;
            }
        }

        return result;
    }
}
