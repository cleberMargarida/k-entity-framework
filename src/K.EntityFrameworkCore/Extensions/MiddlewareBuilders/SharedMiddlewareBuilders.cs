using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring CircuitBreakerMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class CircuitBreakerBuilder<T> where T : class
{
    private readonly CircuitBreakerMiddlewareOptions<T> _options;

    internal CircuitBreakerBuilder(CircuitBreakerMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        _options.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        _options.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        _options.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        _options.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        _options.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when the circuit breaker opens.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> OnCircuitOpened(Action action)
    {
        _options.OnCircuitOpened = action;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when the circuit breaker closes.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The builder instance.</returns>
    public CircuitBreakerBuilder<T> OnCircuitClosed(Action action)
    {
        _options.OnCircuitClosed = action;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring ThrottleMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ThrottleBuilder<T> where T : class
{
    private readonly ThrottleMiddlewareOptions<T> _options;

    internal ThrottleBuilder(ThrottleMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the maximum number of concurrent executions allowed.
    /// </summary>
    /// <param name="maxConcurrency">The maximum concurrency.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithMaxConcurrency(int maxConcurrency)
    {
        _options.MaxConcurrency = maxConcurrency;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of requests per time window.
    /// </summary>
    /// <param name="maxRequests">The maximum requests per window.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithMaxRequestsPerWindow(int maxRequests)
    {
        _options.MaxRequestsPerWindow = maxRequests;
        return this;
    }

    /// <summary>
    /// Sets the time window for rate limiting.
    /// </summary>
    /// <param name="timeWindow">The time window.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithTimeWindow(TimeSpan timeWindow)
    {
        _options.TimeWindow = timeWindow;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait for throttling to allow execution.
    /// </summary>
    /// <param name="maxWaitTime">The maximum wait time.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithMaxWaitTime(TimeSpan maxWaitTime)
    {
        _options.MaxWaitTime = maxWaitTime;
        return this;
    }

    /// <summary>
    /// Sets the throttling strategy to use.
    /// </summary>
    /// <param name="strategy">The throttling strategy.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithStrategy(ThrottlingStrategy strategy)
    {
        _options.Strategy = strategy;
        return this;
    }

    /// <summary>
    /// Enables or disables request queueing when throttling limit is reached.
    /// </summary>
    /// <param name="queueRequests">Whether to queue requests.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithQueueRequests(bool queueRequests = true)
    {
        _options.QueueRequests = queueRequests;
        return this;
    }

    /// <summary>
    /// Sets the maximum queue size for throttled requests.
    /// </summary>
    /// <param name="maxQueueSize">The maximum queue size.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithMaxQueueSize(int maxQueueSize)
    {
        _options.MaxQueueSize = maxQueueSize;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when throttling is triggered.
    /// </summary>
    /// <param name="onThrottled">The action to execute when throttled.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> OnThrottled(Action<string> onThrottled)
    {
        _options.OnThrottled = onThrottled;
        return this;
    }

    /// <summary>
    /// Sets a custom key generator for partitioned throttling.
    /// </summary>
    /// <param name="keyGenerator">The key generator function.</param>
    /// <returns>The builder instance.</returns>
    public ThrottleBuilder<T> WithKeyGenerator(Func<object, string> keyGenerator)
    {
        _options.KeyGenerator = keyGenerator;
        return this;
    }
}
