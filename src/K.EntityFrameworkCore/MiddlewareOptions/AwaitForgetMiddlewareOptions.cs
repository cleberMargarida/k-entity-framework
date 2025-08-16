namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the AwaitForgetMiddleware.
/// </summary>
public class AwaitForgetMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the timeout duration for awaiting the message processing before forgetting.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
