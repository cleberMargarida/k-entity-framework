namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Represents the states of the consumer circuit breaker.
/// </summary>
internal enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation. Failures are tracked in the sliding window.
    /// </summary>
    Closed,

    /// <summary>
    /// Consumption is paused, waiting for the reset interval to elapse.
    /// </summary>
    Open,

    /// <summary>
    /// Probing with a single message to test whether recovery has occurred.
    /// </summary>
    HalfOpen
}
