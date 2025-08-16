namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the InboxMiddleware.
/// </summary>
public class InboxMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the timeout for duplicate message detection.
    /// Messages older than this timeout will be considered safe to process again.
    /// Default is 24 hours.
    /// </summary>
    public TimeSpan DuplicateDetectionTimeout { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the interval for automatic cleanup operations.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
