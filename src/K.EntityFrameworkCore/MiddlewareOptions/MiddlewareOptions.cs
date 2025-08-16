namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Base class for all middleware configuration options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public abstract class MiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets whether the middleware is enabled.
    /// Default is false.
    /// </summary>
    public bool IsMiddlewareEnabled { get; set; } = false;
}
