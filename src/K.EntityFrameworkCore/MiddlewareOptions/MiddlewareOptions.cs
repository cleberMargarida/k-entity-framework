namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Base class for all options used.
/// </summary>
/// <typeparam name="T"></typeparam>
public class OptionsBase<T>
{
}

/// <summary>
/// Base class for all middleware configuration options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MiddlewareOptions<T>(bool isMiddlewareEnabled) : OptionsBase<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MiddlewareOptions{T}"/> class with the specified middleware enabled state.
    /// </summary>
    public MiddlewareOptions() : this(false)
    {
    }

    /// <summary>
    /// Gets or sets whether the middleware is enabled.
    /// Default is false.
    /// </summary>
    public bool IsMiddlewareEnabled { get; set; } = isMiddlewareEnabled;
}
