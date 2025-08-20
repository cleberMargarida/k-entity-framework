namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class CircuitBreakerMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the number of consecutive failures required to trip the circuit breaker.
    /// Default is 5.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the time to wait before attempting to reset the circuit breaker.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the time window for counting failures.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the minimum number of requests required before the circuit breaker can trip.
    /// Default is 10.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets the exception types that should count as failures.
    /// If null or empty, all exceptions count as failures.
    /// </summary>
    public Type[]? ExceptionTypesToBreakOn { get; set; }
}
