namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Contains annotation keys used to store middleware settings in the Entity Framework model.
/// </summary>
internal static class ModelAnnotationKeys
{
    private const string BasePrefix = "K.EntityFrameworkCore.";

    /// <summary>
    /// Annotation key for storing ClientSettings by message type.
    /// </summary>
    public static string ClientSettings(Type messageType) => $"{BasePrefix}ClientSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the topic name for a message type.
    /// </summary>
    public static string TopicName(Type messageType) => $"{BasePrefix}TopicName[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing ProducerMiddlewareSettings by message type.
    /// </summary>
    public static string ProducerMiddlewareSettings(Type messageType) => $"{BasePrefix}ProducerMiddlewareSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the key property accessor for a producer message type.
    /// </summary>
    public static string ProducerKeyPropertyAccessor(Type messageType) => $"{BasePrefix}ProducerKeyPropertyAccessor[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing whether a producer message type has no key.
    /// </summary>
    public static string ProducerHasNoKey(Type messageType) => $"{BasePrefix}ProducerHasNoKey[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing SerializationMiddlewareSettings by message type.
    /// </summary>
    public static string SerializationMiddlewareSettings(Type messageType) => $"{BasePrefix}SerializationMiddlewareSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the serializer type for a message type.
    /// </summary>
    public static string SerializerInstance(Type messageType) => $"{BasePrefix}SerializerType[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing OutboxMiddlewareSettings by message type.
    /// </summary>
    public static string OutboxMiddlewareSettings(Type messageType) => $"{BasePrefix}OutboxMiddlewareSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing whether outbox middleware is enabled for a message type.
    /// </summary>
    public static string OutboxEnabled(Type messageType) => $"{BasePrefix}OutboxEnabled[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the producing strategy for an outbox message type.
    /// </summary>
    public static string OutboxPublishingStrategy(Type messageType) => $"{BasePrefix}OutboxPublishingStrategy[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing ProducerForgetMiddlewareSettings by message type.
    /// </summary>
    public static string ProducerForgetMiddlewareSettings(Type messageType) => $"{BasePrefix}ProducerForgetMiddlewareSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing whether producer forget middleware is enabled for a message type.
    /// </summary>
    public static string ProducerForgetEnabled(Type messageType) => $"{BasePrefix}ProducerForgetEnabled[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the forget strategy for a producer message type.
    /// </summary>
    public static string ProducerForgetStrategy(Type messageType) => $"{BasePrefix}ProducerForgetStrategy[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the forget timeout for a producer message type.
    /// </summary>
    public static string ProducerForgetTimeout(Type messageType) => $"{BasePrefix}ProducerForgetTimeout[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing InboxMiddlewareSettings by message type.
    /// </summary>
    public static string InboxMiddlewareSettings(Type messageType) => $"{BasePrefix}InboxMiddlewareSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing whether inbox middleware is enabled for a message type.
    /// </summary>
    public static string InboxEnabled(Type messageType) => $"{BasePrefix}InboxEnabled[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the deduplication value accessor for an inbox message type.
    /// </summary>
    public static string InboxDeduplicationValueAccessor(Type messageType) => $"{BasePrefix}InboxDeduplicationValueAccessor[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the deduplication time window for an inbox message type.
    /// </summary>
    public static string InboxDeduplicationTimeWindow(Type messageType) => $"{BasePrefix}InboxDeduplicationTimeWindow[{messageType.FullName}]";


    /// <summary>
    /// Annotation key for storing ConsumerMiddlewareSettings by message type.
    /// </summary>
    public static string ConsumerMiddlewareSettings(Type messageType) => $"{BasePrefix}ConsumerMiddlewareSettings[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the maximum buffered messages for a consumer message type.
    /// </summary>
    public static string ConsumerMaxBufferedMessages(Type messageType) => $"{BasePrefix}ConsumerMaxBufferedMessages[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the backpressure mode for a consumer message type.
    /// </summary>
    public static string ConsumerBackpressureMode(Type messageType) => $"{BasePrefix}ConsumerBackpressureMode[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing whether a consumer message type uses exclusive connection.
    /// </summary>
    public static string ConsumerExclusiveConnection(Type messageType) => $"{BasePrefix}ConsumerExclusiveConnection[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the exclusive connection configuration for a consumer message type.
    /// </summary>
    public static string ConsumerExclusiveConnectionConfig(Type messageType) => $"{BasePrefix}ConsumerExclusiveConnectionConfig[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the high water mark ratio for a consumer message type.
    /// </summary>
    public static string ConsumerHighWaterMarkRatio(Type messageType) => $"{BasePrefix}ConsumerHighWaterMarkRatio[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the low water mark ratio for a consumer message type.
    /// </summary>
    public static string ConsumerLowWaterMarkRatio(Type messageType) => $"{BasePrefix}ConsumerLowWaterMarkRatio[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing header property accessors for a producer message type.
    /// </summary>
    public static string ProducerHeaderAccessors(Type messageType) => $"{BasePrefix}ProducerHeaderAccessors[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing header filter expressions for a consumer message type.
    /// </summary>
    public static string ConsumerHeaderFilters(Type messageType) => $"{BasePrefix}ConsumerHeaderFilters[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing topic specifications for a message type.
    /// </summary>
    public static string TopicSpecification(Type messageType) => $"{BasePrefix}TopicSpecification[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing the outbox polling interval (global, not per-type).
    /// </summary>
    public const string OutboxPollingInterval = BasePrefix + "OutboxPollingInterval";

    /// <summary>
    /// Annotation key for storing the maximum messages per outbox poll (global, not per-type).
    /// </summary>
    public const string OutboxMaxMessagesPerPoll = BasePrefix + "OutboxMaxMessagesPerPoll";

    /// <summary>
    /// Annotation key for storing the outbox coordination strategy (global, not per-type).
    /// </summary>
    public const string OutboxCoordinationStrategy = BasePrefix + "OutboxCoordinationStrategy";

    /// <summary>
    /// Annotation key for storing the circuit breaker configuration for a consumer message type.
    /// </summary>
    public static string ConsumerCircuitBreakerConfig(Type messageType) => $"{BasePrefix}ConsumerCircuitBreakerConfig[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing user-registered producer middleware types for a message type.
    /// </summary>
    public static string UserProducerMiddlewares(Type messageType) => $"{BasePrefix}UserProducerMiddlewares[{messageType.FullName}]";

    /// <summary>
    /// Annotation key for storing user-registered consumer middleware types for a message type.
    /// </summary>
    public static string UserConsumerMiddlewares(Type messageType) => $"{BasePrefix}UserConsumerMiddlewares[{messageType.FullName}]";
}
