using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring InboxMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxBuilder<T>(InboxMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the timeout for duplicate message detection.
    /// </summary>
    /// <param name="timeout">The duplicate detection timeout.</param>
    /// <returns>The builder instance.</returns>
    public InboxBuilder<T> WithDuplicateDetectionTimeout(TimeSpan timeout)
    {
        options.DuplicateDetectionTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer RetryMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerRetryBuilder<T>(ConsumerRetryMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxAttempts(int maxAttempts)
    {
        options.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBaseDelay(TimeSpan delay)
    {
        options.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithMaxDelay(TimeSpan maxDelay)
    {
        options.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBackoffStrategy(RetryBackoffStrategy strategy)
    {
        options.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithBackoffMultiplier(double multiplier)
    {
        options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithJitter(bool useJitter = true)
    {
        options.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetriableExceptions(params Type[] exceptionTypes)
    {
        options.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> WithRetryPredicate(Func<Exception, bool> predicate)
    {
        options.ShouldRetryPredicate = predicate;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute before each retry attempt.
    /// </summary>
    /// <param name="onRetry">The action to execute on retry.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerRetryBuilder<T> OnRetry(Action<int, Exception> onRetry)
    {
        options.OnRetry = onRetry;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer CircuitBreakerMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerCircuitBreakerBuilder<T>(ConsumerCircuitBreakerMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        options.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        options.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        options.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        options.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        options.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when the circuit breaker opens.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> OnCircuitOpened(Action action)
    {
        options.OnCircuitOpened = action;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when the circuit breaker closes.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerCircuitBreakerBuilder<T> OnCircuitClosed(Action action)
    {
        options.OnCircuitClosed = action;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer ThrottleMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerThrottleBuilder<T>(ConsumerThrottleMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of concurrent executions allowed.
    /// </summary>
    /// <param name="maxConcurrency">The maximum concurrency.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithMaxConcurrency(int maxConcurrency)
    {
        options.MaxConcurrency = maxConcurrency;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of requests per time window.
    /// </summary>
    /// <param name="maxRequests">The maximum requests per window.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithMaxRequestsPerWindow(int maxRequests)
    {
        options.MaxRequestsPerWindow = maxRequests;
        return this;
    }

    /// <summary>
    /// Sets the time window for rate limiting.
    /// </summary>
    /// <param name="timeWindow">The time window.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithTimeWindow(TimeSpan timeWindow)
    {
        options.TimeWindow = timeWindow;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait for throttling to allow execution.
    /// </summary>
    /// <param name="maxWaitTime">The maximum wait time.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithMaxWaitTime(TimeSpan maxWaitTime)
    {
        options.MaxWaitTime = maxWaitTime;
        return this;
    }

    /// <summary>
    /// Sets the throttling strategy to use.
    /// </summary>
    /// <param name="strategy">The throttling strategy.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithStrategy(ThrottlingStrategy strategy)
    {
        options.Strategy = strategy;
        return this;
    }

    /// <summary>
    /// Enables or disables request queueing when throttling limit is reached.
    /// </summary>
    /// <param name="queueRequests">Whether to queue requests.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithQueueRequests(bool queueRequests = true)
    {
        options.QueueRequests = queueRequests;
        return this;
    }

    /// <summary>
    /// Sets the maximum queue size for throttled requests.
    /// </summary>
    /// <param name="maxQueueSize">The maximum queue size.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithMaxQueueSize(int maxQueueSize)
    {
        options.MaxQueueSize = maxQueueSize;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when throttling is triggered.
    /// </summary>
    /// <param name="onThrottled">The action to execute when throttled.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> OnThrottled(Action<string> onThrottled)
    {
        options.OnThrottled = onThrottled;
        return this;
    }

    /// <summary>
    /// Sets a custom key generator for partitioned throttling.
    /// </summary>
    /// <param name="keyGenerator">The key generator function.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerThrottleBuilder<T> WithKeyGenerator(Func<object, string> keyGenerator)
    {
        options.KeyGenerator = keyGenerator;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer BatchMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerBatchBuilder<T>(ConsumerBatchMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of messages to batch together.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithBatchSize(int batchSize)
    {
        options.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithBatchTimeout(TimeSpan timeout)
    {
        options.BatchTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables parallel processing of batches.
    /// </summary>
    /// <param name="processInParallel">Whether to process in parallel.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithParallelProcessing(bool processInParallel = true)
    {
        options.ProcessInParallel = processInParallel;
        return this;
    }

    /// <summary>
    /// Sets the maximum degree of parallelism when parallel processing is enabled.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerBatchBuilder<T> WithMaxDegreeOfParallelism(int maxDegreeOfParallelism)
    {
        options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer AwaitForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerAwaitForgetBuilder<T> where T : class
{
    private readonly ConsumerAwaitForgetMiddlewareOptions<T> _options;

    internal ConsumerAwaitForgetBuilder(ConsumerAwaitForgetMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the timeout duration for awaiting message processing.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerAwaitForgetBuilder<T> WithTimeout(TimeSpan timeout)
    {
        _options.Timeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer FireForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerFireForgetBuilder<T> where T : class
{
    private readonly ConsumerFireForgetMiddlewareOptions<T> _options;

    internal ConsumerFireForgetBuilder(ConsumerFireForgetMiddlewareOptions<T> options)
    {
        _options = options;
    }

    // ConsumerFireForgetMiddlewareOptions<T> is currently empty, but we can add methods as needed
}
