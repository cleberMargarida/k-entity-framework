namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Configuration options for the consumer circuit breaker.
/// </summary>
public interface ICircuitBreakerConfig
{
    /// <summary>
    /// Gets the number of failures within the sliding window required to trip the circuit breaker.
    /// Default is 5.
    /// </summary>
    int TripThreshold { get; }

    /// <summary>
    /// Gets the size of the sliding window that tracks recent outcomes.
    /// Default is 10.
    /// </summary>
    int WindowSize { get; }

    /// <summary>
    /// Gets the number of consecutive successes required in the HalfOpen state to close the circuit.
    /// Default is 1.
    /// </summary>
    int ActiveThreshold { get; }

    /// <summary>
    /// Gets the amount of time to wait in the Open state before transitioning to HalfOpen.
    /// Default is 30 seconds.
    /// </summary>
    TimeSpan ResetInterval { get; }
}
