using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring InboxMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxBuilder<T>(InboxMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the timeout for duplicate message detection.
    /// </summary>
    /// <param name="timeout">The duplicate detection timeout.</param>
    /// <returns>The builder instance.</returns>
    public InboxBuilder<T> WithDuplicateDetectionTimeout(TimeSpan timeout)
    {
        options.DuplicateDetectionTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer RetryMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerRetryBuilder<T>(ConsumerRetryMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxAttempts(int maxAttempts)
    {
        options.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBaseDelay(TimeSpan delay)
    {
        options.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxDelay(TimeSpan maxDelay)
    {
        options.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBackoffStrategy(RetryBackoffStrategy strategy)
    {
        options.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBackoffMultiplier(double multiplier)
    {
        options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithJitter(bool useJitter = true)
    {
        options.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetriableExceptions(params Type[] exceptionTypes)
    {
        options.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryPredicate(Func<Exception, bool> predicate)
    {
        options.ShouldRetryPredicate = predicate;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer CircuitBreakerMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerCircuitBreakerBuilder<T>(ConsumerCircuitBreakerMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        options.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        options.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        options.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        options.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        options.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer BatchMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerBatchBuilder<T>(ConsumerBatchMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of messages to batch together.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithBatchSize(int batchSize)
    {
        options.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithBatchTimeout(TimeSpan timeout)
    {
        options.BatchTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer ForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerForgetBuilder<T> where T : class
{
    private readonly ConsumerForgetMiddlewareOptions<T> _options;

    internal ConsumerForgetBuilder(ConsumerForgetMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the forget strategy to AwaitForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> UseAwaitForget()
    {
        _options.Strategy = ForgetStrategy.AwaitForget;
        return this;
    }

    /// <summary>
    /// Sets the forget strategy to FireForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> UseFireForget()
    {
        _options.Strategy = ForgetStrategy.FireForget;
        return this;
    }

    /// <summary>
    /// Sets the timeout duration for awaiting message processing.
    /// Only applies when using AwaitForget strategy.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithTimeout(TimeSpan timeout)
    {
        _options.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures the middleware for AwaitForget strategy with optional timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration for awaiting processing.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithAwaitForget(TimeSpan? timeout = null)
    {
        _options.Strategy = ForgetStrategy.AwaitForget;
        if (timeout.HasValue)
        {
            _options.Timeout = timeout.Value;
        }
        return this;
    }

    /// <summary>
    /// Configures the middleware for FireForget strategy.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithFireForget()
    {
        _options.Strategy = ForgetStrategy.FireForget;
        return this;
    }
}
