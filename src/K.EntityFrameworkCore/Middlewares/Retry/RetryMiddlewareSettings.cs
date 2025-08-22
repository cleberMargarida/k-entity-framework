using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Retry;

/// <summary>
/// Configuration options for the RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class RetryMiddlewareSettings<T> 
    : MiddlewareSettings<T>
    , IRetryMiddlewareSettings
    where T : class
{
    /// <inheritdoc/>
    public int RetryBackoffMaxMilliseconds { get; set; }

    /// <inheritdoc/>
    public int RetryBackoffMilliseconds { get; set; }

    /// <inheritdoc/>
    public int MaxRetries { get; set; }
}

