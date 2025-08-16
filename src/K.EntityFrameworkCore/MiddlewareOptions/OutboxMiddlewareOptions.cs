namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the OutboxMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxMiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the interval for polling and publishing outbox messages.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the outbox publishing strategy.
    /// </summary>
    public OutboxPublishingStrategy Strategy { get; set; } = OutboxPublishingStrategy.ImmediateWithFallback;
}

/// <summary>
/// Defines the publishing strategy for outbox messages.
/// </summary>
public enum OutboxPublishingStrategy
{
    /// <summary>
    /// Always publish messages in the background after saving.
    /// Messages are processed during the next polling cycle.
    /// </summary>
    BackgroundOnly,

    /// <summary>
    /// Publish immediately after saving. If successful, remove the message.
    /// If immediate publishing fails, fall back to background processing.
    /// </summary>
    ImmediateWithFallback,

}
