using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Retry;

/// <summary>
/// Configuration options for the RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class RetryMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay between retry attempts.
    /// Default is 1 second.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the backoff strategy for calculating retry delays.
    /// Default is Exponential.
    /// </summary>
    public RetryBackoffStrategy BackoffStrategy { get; set; } = RetryBackoffStrategy.Exponential;

    /// <summary>
    /// Gets or sets the backoff multiplier for exponential backoff.
    /// Default is 2.0.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets whether to add jitter to retry delays to avoid thundering herd.
    /// Default is true.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets or sets the exception types that should trigger a retry.
    /// If null or empty, all exceptions trigger retries.
    /// </summary>
    public Type[]? RetriableExceptionTypes { get; set; }

    /// <summary>
    /// Gets or sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    public Func<Exception, bool>? ShouldRetryPredicate { get; set; }
}

/// <summary>
/// Defines the backoff strategy for retry delays.
/// </summary>
public enum RetryBackoffStrategy
{
    /// <summary>
    /// Linear backoff: delay = baseDelay * attemptNumber
    /// </summary>
    Linear,

    /// <summary>
    /// Exponential backoff: delay = baseDelay * (multiplier ^ attemptNumber)
    /// </summary>
    Exponential,

    /// <summary>
    /// Fixed delay: delay = baseDelay
    /// </summary>
    Fixed
}
