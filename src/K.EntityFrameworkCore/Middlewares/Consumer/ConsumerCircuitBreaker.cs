using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Thread-safe state machine implementing the circuit breaker pattern for consumers.
/// </summary>
internal sealed class ConsumerCircuitBreaker
{
    private readonly object _lock = new();
    private readonly SlidingWindow _window;
    private readonly ICircuitBreakerConfig _config;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTimeOffset _openedAt;
    private int _halfOpenSuccesses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerCircuitBreaker"/> class.
    /// </summary>
    /// <param name="config">The circuit breaker configuration.</param>
    public ConsumerCircuitBreaker(ICircuitBreakerConfig config)
    {
        _config = config;
        _window = new SlidingWindow(config.WindowSize);
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Checks whether a request should be allowed through the circuit breaker.
    /// Transitions from Open to HalfOpen when the reset interval has elapsed.
    /// </summary>
    /// <returns>True if the request is allowed; false if the circuit is open.</returns>
    public bool AllowRequest()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;

                case CircuitBreakerState.Open:
                    if (DateTimeOffset.UtcNow - _openedAt >= _config.ResetInterval)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _halfOpenSuccesses = 0;
                        return true;
                    }
                    return false;

                case CircuitBreakerState.HalfOpen:
                    return true;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Records a successful outcome. In HalfOpen state, transitions to Closed
    /// when the active threshold is met.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.HalfOpen:
                    _halfOpenSuccesses++;
                    if (_halfOpenSuccesses >= _config.ActiveThreshold)
                    {
                        _state = CircuitBreakerState.Closed;
                        _window.Reset();
                    }
                    break;

                case CircuitBreakerState.Closed:
                    _window.Record(false);
                    break;
            }
        }
    }

    /// <summary>
    /// Records a failure outcome. In Closed state, trips the circuit when the threshold
    /// within the sliding window is met. In HalfOpen state, immediately transitions back to Open.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    _window.Record(true);
                    if (_window.FailureCount >= _config.TripThreshold)
                    {
                        _state = CircuitBreakerState.Open;
                        _openedAt = DateTimeOffset.UtcNow;
                    }
                    break;

                case CircuitBreakerState.HalfOpen:
                    _state = CircuitBreakerState.Open;
                    _openedAt = DateTimeOffset.UtcNow;
                    break;
            }
        }
    }
}
