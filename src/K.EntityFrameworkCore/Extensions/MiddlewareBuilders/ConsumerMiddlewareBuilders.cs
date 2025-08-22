using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Retry;

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
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxRetries(int maxRetries)
    {
        settings.MaxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the base backoff time in milliseconds before retrying.
    /// </summary>
    /// <param name="milliseconds">The backoff time in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryBackoffMilliseconds(int milliseconds)
    {
        settings.RetryBackoffMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the base backoff time using a TimeSpan.
    /// </summary>
    /// <param name="delay">The backoff time as a TimeSpan.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryBackoff(TimeSpan delay)
    {
        settings.RetryBackoffMilliseconds = (int)delay.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum backoff time in milliseconds before retrying.
    /// </summary>
    /// <param name="milliseconds">The maximum backoff time in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryBackoffMaxMilliseconds(int milliseconds)
    {
        settings.RetryBackoffMaxMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum backoff time using a TimeSpan.
    /// </summary>
    /// <param name="maxDelay">The maximum backoff time as a TimeSpan.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryBackoffMax(TimeSpan maxDelay)
    {
        settings.RetryBackoffMaxMilliseconds = (int)maxDelay.TotalMilliseconds;
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
