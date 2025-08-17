namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ForgetMiddlewareOptions<T> : MiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the execution strategy for the forget middleware.
    /// Default is AwaitForget.
    /// </summary>
    public ForgetStrategy Strategy { get; set; } = ForgetStrategy.AwaitForget;

    /// <summary>
    /// Gets or sets the timeout duration for awaiting the message processing before forgetting.
    /// Only applies when Strategy is AwaitForget.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
