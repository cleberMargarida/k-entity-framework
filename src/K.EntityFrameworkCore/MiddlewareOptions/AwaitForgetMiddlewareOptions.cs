namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the AwaitForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class AwaitForgetMiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the timeout duration for awaiting the message processing before forgetting.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
