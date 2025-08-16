namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the ThrottleMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ThrottleMiddlewareOptions<T> : MiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent executions allowed.
    /// Default is 10.
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of requests per time window.
    /// Default is 100.
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window for rate limiting.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the maximum time to wait for throttling to allow execution.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the throttling strategy to use.
    /// Default is Semaphore.
    /// </summary>
    public ThrottlingStrategy Strategy { get; set; } = ThrottlingStrategy.Semaphore;

    /// <summary>
    /// Gets or sets whether to queue requests when throttling limit is reached.
    /// Default is true.
    /// </summary>
    public bool QueueRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum queue size for throttled requests.
    /// Default is 1000.
    /// </summary>
    public int MaxQueueSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a custom action to execute when throttling is triggered.
    /// </summary>
    public Action<string>? OnThrottled { get; set; }

    /// <summary>
    /// Gets or sets a custom key generator for partitioned throttling.
    /// If null, all requests share the same throttling limit.
    /// </summary>
    public Func<object, string>? KeyGenerator { get; set; }
}

/// <summary>
/// Defines the throttling strategy.
/// </summary>
public enum ThrottlingStrategy
{
    /// <summary>
    /// Use a semaphore for concurrency control.
    /// </summary>
    Semaphore,

    /// <summary>
    /// Use token bucket algorithm for rate limiting.
    /// </summary>
    TokenBucket,

    /// <summary>
    /// Use sliding window for rate limiting.
    /// </summary>
    SlidingWindow,

    /// <summary>
    /// Use fixed window for rate limiting.
    /// </summary>
    FixedWindow
}
