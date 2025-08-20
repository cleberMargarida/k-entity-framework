using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.CircuitBreaker;

[ScopedService]
internal abstract class CircuitBreakerMiddleware<T>(CircuitBreakerMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    private static readonly CircuitBreakerState circuitState = new();

#if NET9_0_OR_GREATER
    private static readonly Lock lockObject = new();
#else
    private static readonly object lockObject = new();
#endif

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        lock (lockObject)
        {
            bool isAttemptResetAllowed = IsAttemptResetAllowed(circuitState);
            switch (circuitState.State)
            {
                case CircuitState.Open when isAttemptResetAllowed:
                    if (isAttemptResetAllowed)
                    {
                        circuitState.TransitionToHalfOpen();
                        break;
                    }
                    throw new CircuitBreakerOpenException("Global circuit breaker is open");

                case CircuitState.HalfOpen:
                    break;

                case CircuitState.Closed:
                default:
                    break;
            }
        }

        try
        {
            await base.InvokeAsync(envelope, cancellationToken);
            CircuitBreakerMiddleware<T>.RecordSuccess();
        }
        catch (Exception ex) when (ShouldCountAsFailure(ex))
        {
            RecordFailure();
            throw;
        }
    }

    private static bool IsAttemptResetAllowed(CircuitBreakerState state)
    {
        return DateTime.UtcNow >= state.NextAttemptTime;
    }

    private bool ShouldCountAsFailure(Exception exception)
    {
        if (settings.ExceptionTypesToBreakOn != null && settings.ExceptionTypesToBreakOn.Length > 0)
        {
            return settings.ExceptionTypesToBreakOn.Any(type => type.IsAssignableFrom(exception.GetType()));
        }

        return true;
    }

    private static void RecordSuccess()
    {
        lock (lockObject)
        {
            circuitState.RecordSuccess();

            if (circuitState.State == CircuitState.HalfOpen)
            {
                circuitState.TransitionToClosed();
            }
        }
    }

    private void RecordFailure()
    {
        lock (lockObject)
        {
            circuitState.RecordFailure();

            if (ShouldTripCircuit())
            {
                circuitState.TransitionToOpen(settings.OpenTimeout);
            }
        }
    }

    private bool ShouldTripCircuit()
    {
        var now = DateTime.UtcNow;
        var windowStart = now - settings.SamplingDuration;

        var (failureCount, totalCount) = circuitState.GetStatisticsInWindow(windowStart);

        if (totalCount < settings.MinimumThroughput)
        {
            return false;
        }

        return failureCount >= settings.FailureThreshold;
    }
}

/// <summary>
/// Represents the current state of a global circuit breaker shared across all types and instances.
/// </summary>
internal class CircuitBreakerState
{
    // Simple collections - locking is handled at the middleware level for global state access
    private readonly Queue<TimeBucket> timeBuckets = new();
    private readonly TimeSpan bucketDuration = TimeSpan.FromSeconds(10);

    public CircuitState State { get; private set; } = CircuitState.Closed;
    public DateTime NextAttemptTime { get; private set; }

    public void RecordSuccess()
    {
        var now = DateTime.UtcNow;
        var bucket = GetOrCreateCurrentBucket(now);
        bucket.SuccessCount++;
    }

    public void RecordFailure()
    {
        var now = DateTime.UtcNow;
        var bucket = GetOrCreateCurrentBucket(now);
        bucket.FailureCount++;
    }

    public void TransitionToOpen(TimeSpan openTimeout)
    {
        State = CircuitState.Open;
        NextAttemptTime = DateTime.UtcNow.Add(openTimeout);
    }

    public void TransitionToHalfOpen()
    {
        State = CircuitState.HalfOpen;
    }

    public void TransitionToClosed()
    {
        State = CircuitState.Closed;
        NextAttemptTime = DateTime.MinValue;
    }

    public (int failureCount, int totalCount) GetStatisticsInWindow(DateTime windowStart)
    {
        CleanupOldBuckets(windowStart);

        int totalFailures = 0;
        int totalCalls = 0;

        foreach (var bucket in timeBuckets)
        {
            if (bucket.StartTime >= windowStart)
            {
                totalFailures += bucket.FailureCount;
                totalCalls += bucket.SuccessCount + bucket.FailureCount;
            }
        }

        return (totalFailures, totalCalls);
    }

    private TimeBucket GetOrCreateCurrentBucket(DateTime now)
    {
        var bucketStart = new DateTime(now.Ticks - now.Ticks % bucketDuration.Ticks);

        if (timeBuckets.Count == 0 || timeBuckets.Last().StartTime < bucketStart)
        {
            timeBuckets.Enqueue(new TimeBucket(bucketStart));
        }

        return timeBuckets.Last();
    }

    private void CleanupOldBuckets(DateTime windowStart)
    {
        while (timeBuckets.Count > 0 && timeBuckets.Peek().StartTime < windowStart)
        {
            timeBuckets.Dequeue();
        }
    }
}

/// <summary>
/// Represents a time bucket for collecting circuit breaker statistics.
/// </summary>
internal class TimeBucket(DateTime startTime)
{
    public DateTime StartTime { get; } = startTime;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// Represents the state of the circuit breaker.
/// </summary>
internal enum CircuitState
{
    /// <summary>
    /// Circuit is closed - normal operation.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open - failing fast.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open - testing recovery.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when the circuit breaker is open.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the CircuitBreakerOpenException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CircuitBreakerOpenException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the CircuitBreakerOpenException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
}
