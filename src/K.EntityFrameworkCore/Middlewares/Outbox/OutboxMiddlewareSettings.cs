using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// Configuration options for the OutboxMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
public class OutboxMiddlewareSettings<T>(IModel model) : MiddlewareSettings<T>(model.IsOutboxEnabled<T>())
    where T : class
{
    /// <summary>
    /// Gets the outbox publishing strategy from model annotations.
    /// Default is BackgroundOnly.
    /// </summary>
    public OutboxPublishingStrategy Strategy => model.GetOutboxPublishingStrategy<T>() ?? OutboxPublishingStrategy.BackgroundOnly;
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
