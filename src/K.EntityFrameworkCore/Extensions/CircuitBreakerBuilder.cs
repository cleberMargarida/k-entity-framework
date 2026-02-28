using K.EntityFrameworkCore.Middlewares.Consumer;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides a fluent API for configuring circuit breaker settings on a consumer.
/// </summary>
public class CircuitBreakerBuilder
{
    private int _tripThreshold = 5;
    private int _windowSize = 10;
    private int _activeThreshold = 1;
    private TimeSpan _resetInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sets the number of failures within the sliding window that will trip the circuit breaker.
    /// Default is 5.
    /// </summary>
    /// <param name="failures">The number of failures to trip on.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder TripAfter(int failures)
    {
        _tripThreshold = failures;
        return this;
    }

    /// <summary>
    /// Sets the size of the sliding window that tracks recent outcomes.
    /// Default is 10.
    /// </summary>
    /// <param name="size">The window size.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder HasWindowSize(int size)
    {
        _windowSize = size;
        return this;
    }

    /// <summary>
    /// Sets the time to wait in the Open state before transitioning to HalfOpen.
    /// Default is 30 seconds.
    /// </summary>
    /// <param name="interval">The reset interval.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder HasResetInterval(TimeSpan interval)
    {
        _resetInterval = interval;
        return this;
    }

    /// <summary>
    /// Sets the number of consecutive successes required in HalfOpen state to close the circuit.
    /// Default is 1.
    /// </summary>
    /// <param name="successes">The active threshold.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder HasActiveThreshold(int successes)
    {
        _activeThreshold = successes;
        return this;
    }

    /// <summary>
    /// Builds the circuit breaker configuration.
    /// </summary>
    /// <returns>The circuit breaker configuration.</returns>
    internal CircuitBreakerConfig Build() => new()
    {
        TripThreshold = _tripThreshold,
        WindowSize = _windowSize,
        ActiveThreshold = _activeThreshold,
        ResetInterval = _resetInterval
    };
}
