using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring CircuitBreakerMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class CircuitBreakerBuilder<T>(CircuitBreakerMiddlewareOptions<T> options) 
    where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        options.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        options.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        options.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        options.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        options.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }
}
