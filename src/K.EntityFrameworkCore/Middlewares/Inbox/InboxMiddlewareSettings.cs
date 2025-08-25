using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

/// <summary>
/// Configuration options for the InboxMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the timeout for duplicate message detection.
    /// Messages older than this timeout will be considered safe to process again.
    /// Default is 24 hours.
    /// </summary>
    public TimeSpan DeduplicationTimeWindow { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the interval for automatic cleanup operations.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    internal Guid Hash(Envelope<T> envelope)
    {
        throw new NotImplementedException();
    }
}
