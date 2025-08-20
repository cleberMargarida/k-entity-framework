using K.EntityFrameworkCore.Middlewares;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring InboxMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxBuilder<T>(InboxMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the timeout for duplicate message detection.
    /// </summary>
    /// <param name="timeout">The duplicate detection timeout.</param>
    /// <returns>The builder instance.</returns>
    public InboxBuilder<T> WithDuplicateDetectionTimeout(TimeSpan timeout)
    {
        settings.DuplicateDetectionTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer RetryMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerRetryBuilder<T>(ConsumerRetryMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxAttempts(int maxAttempts)
    {
        settings.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBaseDelay(TimeSpan delay)
    {
        settings.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxDelay(TimeSpan maxDelay)
    {
        settings.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBackoffStrategy(RetryBackoffStrategy strategy)
    {
        settings.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBackoffMultiplier(double multiplier)
    {
        settings.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithJitter(bool useJitter = true)
    {
        settings.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetriableExceptions(params Type[] exceptionTypes)
    {
        settings.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryPredicate(Func<Exception, bool> predicate)
    {
        settings.ShouldRetryPredicate = predicate;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer CircuitBreakerMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerCircuitBreakerBuilder<T>(ConsumerCircuitBreakerMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        settings.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        settings.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        settings.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        settings.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        settings.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer BatchMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerBatchBuilder<T>(ConsumerBatchMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the maximum number of messages to batch together.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithBatchSize(int batchSize)
    {
        settings.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithBatchTimeout(TimeSpan timeout)
    {
        settings.BatchTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer ForgetMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerForgetBuilder<T>(ConsumerForgetMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the forget strategy to AwaitForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> UseAwaitForget()
    {
        settings.Strategy = ForgetStrategy.AwaitForget;
        return this;
    }

    /// <summary>
    /// Sets the forget strategy to FireForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> UseFireForget()
    {
        settings.Strategy = ForgetStrategy.FireForget;
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
        settings.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures the middleware for AwaitForget strategy with optional timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration for awaiting processing.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithAwaitForget(TimeSpan? timeout = null)
    {
        settings.Strategy = ForgetStrategy.AwaitForget;
        if (timeout.HasValue)
        {
            settings.Timeout = timeout.Value;
        }
        return this;
    }

    /// <summary>
    /// Configures the middleware for FireForget strategy.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithFireForget()
    {
        settings.Strategy = ForgetStrategy.FireForget;
        return this;
    }
}
