namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class BatchMiddlewareOptions<T> : MiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the maximum number of messages to batch together.
    /// Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum time to wait before processing a batch, even if it's not full.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
