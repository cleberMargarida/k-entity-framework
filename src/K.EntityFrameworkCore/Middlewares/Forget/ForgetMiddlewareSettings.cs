using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Forget;

/// <summary>
/// Configuration options for the ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ForgetMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForgetMiddlewareSettings{T}"/> with middleware disabled.
    /// </summary>
    public ForgetMiddlewareSettings() : base(false) { }

    /// <summary>
    /// Initializes a new instance of <see cref="ForgetMiddlewareSettings{T}"/> with the specified enabled state.
    /// </summary>
    /// <param name="isMiddlewareEnabled">Whether the middleware is enabled.</param>
    public ForgetMiddlewareSettings(bool isMiddlewareEnabled) : base(isMiddlewareEnabled) { }

    /// <summary>
    /// Gets or sets the execution strategy for the forget middleware.
    /// Default is AwaitForget.
    /// </summary>
    public virtual ForgetStrategy Strategy { get; set; } = ForgetStrategy.AwaitForget;

    /// <summary>
    /// Gets or sets the timeout duration for awaiting the message processing before forgetting.
    /// Only applies when Strategy is AwaitForget.
    /// Default is 30 seconds.
    /// </summary>
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
