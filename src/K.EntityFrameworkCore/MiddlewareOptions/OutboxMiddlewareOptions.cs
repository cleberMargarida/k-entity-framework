namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the OutboxMiddleware.
/// </summary>
public class OutboxMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the interval for polling and publishing outbox messages.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /*
     
    - publish immediatelly after saving, if success, remove it.
    - publish in background after saving.

     */
}
