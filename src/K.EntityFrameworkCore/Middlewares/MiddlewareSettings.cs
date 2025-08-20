namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Base class for all options used.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SettingsBase<T>
{
}

/// <summary>
/// Base class for all middleware configuration settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MiddlewareSettings<T>(bool isMiddlewareEnabled) : SettingsBase<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MiddlewareSettings{T}"/> class with the specified middleware enabled state.
    /// </summary>
    public MiddlewareSettings() : this(false)
    {
    }

    /// <summary>
    /// Gets or sets whether the middleware is enabled.
    /// Default is false.
    /// </summary>
    public bool IsMiddlewareEnabled { get; set; } = isMiddlewareEnabled;

    /// <summary>
    /// Enables the middleware.
    /// </summary>
    public void EnableMiddleware() => IsMiddlewareEnabled = true;
}
