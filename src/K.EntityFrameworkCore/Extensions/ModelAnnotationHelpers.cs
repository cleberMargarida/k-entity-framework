using K.EntityFrameworkCore.Middlewares.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Helper methods for working with middleware settings stored as model annotations.
/// </summary>
internal static class ModelAnnotationHelpers
{
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

    /// <summary>
    /// Sets the cleanup interval for an inbox message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <param name="cleanupInterval">The cleanup interval.</param>
    public static void SetInboxCleanupInterval<T>(this IMutableModel model, TimeSpan cleanupInterval)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxCleanupInterval(typeof(T));
        model.SetAnnotation(annotationKey, cleanupInterval);
    }

    /// <summary>
    /// Gets the cleanup interval for an inbox message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The cleanup interval, or null if not set.</returns>
    public static TimeSpan? GetInboxCleanupInterval<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.InboxCleanupInterval(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as TimeSpan?;
    }

    /// <summary>
    /// Sets the serializer type for a message type in the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <typeparam name="TSerializer">The serializer type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    public static void SetSerializer<T, TSerializer>(this IMutableModel model)
        where T : class
        where TSerializer : class
    {
        var annotationKey = ModelAnnotationKeys.SerializerType(typeof(T));
        model.SetAnnotation(annotationKey, typeof(TSerializer));
    }

    /// <summary>
    /// Gets the serializer type for a message type from the model annotations.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="model">The Entity Framework model.</param>
    /// <returns>The serializer type, or null if not set.</returns>
    public static Type? GetSerializerType<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.SerializerType(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value as Type;
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
    public static void SetBackpressureMode<T>(this IMutableModel model, object mode)
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
    public static object? GetBackpressureMode<T>(this IModel model)
        where T : class
    {
        var annotationKey = ModelAnnotationKeys.ConsumerBackpressureMode(typeof(T));
        return model.FindAnnotation(annotationKey)?.Value;
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
}
