using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// Configuration options for the OutboxMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the outbox publishing strategy.
    /// Default is ImmediateWithFallback.
    /// </summary>
    public OutboxPublishingStrategy Strategy { get; set; } = OutboxPublishingStrategy.BackgroundOnly;
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
