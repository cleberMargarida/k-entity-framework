using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring OutboxMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxBuilder<T> where T : class
{
    private readonly OutboxMiddlewareOptions<T> _options;

    internal OutboxBuilder(OutboxMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the polling interval for publishing outbox messages.
    /// </summary>
    /// <param name="interval">The polling interval.</param>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> WithPollingInterval(TimeSpan interval)
    {
        _options.PollingInterval = interval;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring RetryMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class RetryBuilder<T> where T : class
{
    private readonly RetryMiddlewareOptions<T> _options;

    internal RetryBuilder(RetryMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithMaxAttempts(int maxAttempts)
    {
        _options.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithBaseDelay(TimeSpan delay)
    {
        _options.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithMaxDelay(TimeSpan maxDelay)
    {
        _options.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithBackoffStrategy(RetryBackoffStrategy strategy)
    {
        _options.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithBackoffMultiplier(double multiplier)
    {
        _options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithJitter(bool useJitter = true)
    {
        _options.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithRetriableExceptions(params Type[] exceptionTypes)
    {
        _options.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithRetryPredicate(Func<Exception, bool> predicate)
    {
        _options.ShouldRetryPredicate = predicate;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute before each retry attempt.
    /// </summary>
    /// <param name="onRetry">The action to execute on retry.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> OnRetry(Action<int, Exception> onRetry)
    {
        _options.OnRetry = onRetry;
        return this;
    }
}
