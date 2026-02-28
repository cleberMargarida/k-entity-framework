using K.EntityFrameworkCore.Middlewares.Consumer;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Consumer;

/// <summary>
/// Tests for <see cref="ConsumerCircuitBreaker"/>.
/// </summary>
public class ConsumerCircuitBreakerTests
{
    private static CircuitBreakerConfig DefaultConfig(
        int tripThreshold = 5,
        int windowSize = 10,
        int activeThreshold = 1,
        TimeSpan? resetInterval = null) => new()
    {
        TripThreshold = tripThreshold,
        WindowSize = windowSize,
        ActiveThreshold = activeThreshold,
        ResetInterval = resetInterval ?? TimeSpan.FromSeconds(30)
    };

    [Fact]
    public void StartsInClosedState()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig());

        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    [Fact]
    public void AllowRequest_Closed_ReturnsTrue()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig());

        Assert.True(cb.AllowRequest());
    }

    [Fact]
    public void ClosedToOpen_WhenFailuresReachThreshold()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 3, windowSize: 5));

        cb.RecordFailure();
        cb.RecordFailure();
        Assert.Equal(CircuitBreakerState.Closed, cb.State);

        cb.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, cb.State);
    }

    [Fact]
    public void Open_AllowRequestReturnsFalse_BeforeResetInterval()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 1, windowSize: 1, resetInterval: TimeSpan.FromSeconds(30)));

        cb.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, cb.State);

        Assert.False(cb.AllowRequest());
        Assert.Equal(CircuitBreakerState.Open, cb.State);
    }

    [Fact]
    public async Task OpenToHalfOpen_AfterResetInterval()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 1, windowSize: 1, resetInterval: TimeSpan.FromMilliseconds(100)));

        cb.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, cb.State);

        await WaitForConditionAsync(() => cb.AllowRequest());

        Assert.Equal(CircuitBreakerState.HalfOpen, cb.State);
    }

    [Fact]
    public async Task HalfOpenToClosed_OnSuccess()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 1, windowSize: 1, resetInterval: TimeSpan.FromMilliseconds(100)));

        cb.RecordFailure();
        await WaitForConditionAsync(() => cb.AllowRequest()); // Transition to HalfOpen

        Assert.Equal(CircuitBreakerState.HalfOpen, cb.State);

        cb.RecordSuccess();
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    [Fact]
    public async Task HalfOpenToOpen_OnFailure()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 1, windowSize: 1, resetInterval: TimeSpan.FromMilliseconds(100)));

        cb.RecordFailure();
        await WaitForConditionAsync(() => cb.AllowRequest()); // Transition to HalfOpen

        Assert.Equal(CircuitBreakerState.HalfOpen, cb.State);

        cb.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, cb.State);
    }

    [Fact]
    public void RecordSuccess_InClosedState_DoesNotChangeState()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig());

        cb.RecordSuccess();
        cb.RecordSuccess();
        cb.RecordSuccess();

        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    [Fact]
    public void WindowTracksOnlyRecentFailures()
    {
        // Window of 5, trip at 3 failures
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 3, windowSize: 5));

        // Record 2 failures and 3 successes (failures are old after wrapping)
        cb.RecordFailure();
        cb.RecordFailure();
        cb.RecordSuccess();
        cb.RecordSuccess();
        cb.RecordSuccess();

        // Window is now [F, F, S, S, S] — only 2 failures, should still be closed
        Assert.Equal(CircuitBreakerState.Closed, cb.State);

        // Add more successes to push out old failures
        cb.RecordSuccess();
        cb.RecordSuccess();

        // Window is now [S, S, S, S, S] — 0 failures
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    [Fact]
    public async Task FullCycle_ClosedToOpenToHalfOpenToClosed()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 2, windowSize: 5, resetInterval: TimeSpan.FromMilliseconds(50)));

        // Closed → Open
        cb.RecordFailure();
        cb.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, cb.State);

        // Open → HalfOpen
        await WaitForConditionAsync(() => cb.AllowRequest());
        Assert.Equal(CircuitBreakerState.HalfOpen, cb.State);

        // HalfOpen → Closed
        cb.RecordSuccess();
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    [Fact]
    public async Task HalfOpenRequiresActiveThresholdSuccesses()
    {
        var cb = new ConsumerCircuitBreaker(DefaultConfig(tripThreshold: 1, windowSize: 1, activeThreshold: 3, resetInterval: TimeSpan.FromMilliseconds(100)));

        cb.RecordFailure();
        await WaitForConditionAsync(() => cb.AllowRequest()); // HalfOpen

        cb.RecordSuccess();
        Assert.Equal(CircuitBreakerState.HalfOpen, cb.State);

        cb.RecordSuccess();
        Assert.Equal(CircuitBreakerState.HalfOpen, cb.State);

        cb.RecordSuccess();
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns <c>true</c> or the timeout elapses.
    /// </summary>
    private static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 5_000, int pollIntervalMs = 10)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
                return;
            await Task.Delay(pollIntervalMs);
        }

        Assert.Fail($"Condition was not met within {timeoutMs}ms.");
    }
}
