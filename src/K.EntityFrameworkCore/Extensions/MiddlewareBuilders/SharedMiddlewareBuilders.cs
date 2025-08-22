using K.EntityFrameworkCore.Middlewares.CircuitBreaker;
using K.EntityFrameworkCore.Middlewares.Retry;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring RetryMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class RetryBuilder<T>(RetryMiddlewareSettings<T> settings) 
    where T : class
{
    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithMaxRetries(int maxRetries)
    {
        settings.MaxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the base backoff time in milliseconds before retrying.
    /// </summary>
    /// <param name="milliseconds">The backoff time in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithRetryBackoffMilliseconds(int milliseconds)
    {
        settings.RetryBackoffMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the base backoff time using a TimeSpan.
    /// </summary>
    /// <param name="delay">The backoff time as a TimeSpan.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithRetryBackoff(TimeSpan delay)
    {
        settings.RetryBackoffMilliseconds = (int)delay.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum backoff time in milliseconds before retrying.
    /// </summary>
    /// <param name="milliseconds">The maximum backoff time in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithRetryBackoffMaxMilliseconds(int milliseconds)
    {
        settings.RetryBackoffMaxMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum backoff time using a TimeSpan.
    /// </summary>
    /// <param name="maxDelay">The maximum backoff time as a TimeSpan.</param>
    /// <returns>The builder instance.</returns>
    public RetryBuilder<T> WithRetryBackoffMax(TimeSpan maxDelay)
    {
        settings.RetryBackoffMaxMilliseconds = (int)maxDelay.TotalMilliseconds;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring CircuitBreakerMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class CircuitBreakerBuilder<T>(CircuitBreakerMiddlewareSettings<T> settings) 
    where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        settings.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        settings.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        settings.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        settings.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        settings.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }
}
