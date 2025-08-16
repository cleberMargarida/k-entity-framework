using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring OutboxMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxBuilder<T>(OutboxMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the polling interval for publishing outbox messages.
    /// </summary>
    /// <param name="interval">The polling interval.</param>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> WithPollingInterval(TimeSpan interval)
    {
        options.PollingInterval = interval;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer RetryMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerRetryBuilder<T>(ProducerRetryMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithMaxAttempts(int maxAttempts)
    {
        options.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithBaseDelay(TimeSpan delay)
    {
        options.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithMaxDelay(TimeSpan maxDelay)
    {
        options.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithBackoffStrategy(RetryBackoffStrategy strategy)
    {
        options.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithBackoffMultiplier(double multiplier)
    {
        options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithJitter(bool useJitter = true)
    {
        options.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithRetriableExceptions(params Type[] exceptionTypes)
    {
        options.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> WithRetryPredicate(Func<Exception, bool> predicate)
    {
        options.ShouldRetryPredicate = predicate;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer CircuitBreakerMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerCircuitBreakerBuilder<T>(ProducerCircuitBreakerMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> WithFailureThreshold(int threshold)
    {
        options.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> WithOpenTimeout(TimeSpan timeout)
    {
        options.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> WithSamplingDuration(TimeSpan duration)
    {
        options.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> WithMinimumThroughput(int throughput)
    {
        options.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> WithBreakOnExceptions(params Type[] exceptionTypes)
    {
        options.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer ThrottleMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerThrottleBuilder<T>(ProducerThrottleMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of concurrent executions allowed.
    /// </summary>
    /// <param name="maxConcurrency">The maximum concurrency.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithMaxConcurrency(int maxConcurrency)
    {
        options.MaxConcurrency = maxConcurrency;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of requests per time window.
    /// </summary>
    /// <param name="maxRequests">The maximum requests per window.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithMaxRequestsPerWindow(int maxRequests)
    {
        options.MaxRequestsPerWindow = maxRequests;
        return this;
    }

    /// <summary>
    /// Sets the time window for rate limiting.
    /// </summary>
    /// <param name="timeWindow">The time window.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithTimeWindow(TimeSpan timeWindow)
    {
        options.TimeWindow = timeWindow;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait for throttling to allow execution.
    /// </summary>
    /// <param name="maxWaitTime">The maximum wait time.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithMaxWaitTime(TimeSpan maxWaitTime)
    {
        options.MaxWaitTime = maxWaitTime;
        return this;
    }

    /// <summary>
    /// Sets the throttling strategy to use.
    /// </summary>
    /// <param name="strategy">The throttling strategy.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithStrategy(ThrottlingStrategy strategy)
    {
        options.Strategy = strategy;
        return this;
    }

    /// <summary>
    /// Enables or disables request queueing when throttling limit is reached.
    /// </summary>
    /// <param name="queueRequests">Whether to queue requests.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithQueueRequests(bool queueRequests = true)
    {
        options.QueueRequests = queueRequests;
        return this;
    }

    /// <summary>
    /// Sets the maximum queue size for throttled requests.
    /// </summary>
    /// <param name="maxQueueSize">The maximum queue size.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithMaxQueueSize(int maxQueueSize)
    {
        options.MaxQueueSize = maxQueueSize;
        return this;
    }

    /// <summary>
    /// Sets a custom action to execute when throttling is triggered.
    /// </summary>
    /// <param name="onThrottled">The action to execute when throttled.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> OnThrottled(Action<string> onThrottled)
    {
        options.OnThrottled = onThrottled;
        return this;
    }

    /// <summary>
    /// Sets a custom key generator for partitioned throttling.
    /// </summary>
    /// <param name="keyGenerator">The key generator function.</param>
    /// <returns>The builder instance.</returns>
    public ProducerThrottleBuilder<T> WithKeyGenerator(Func<object, string> keyGenerator)
    {
        options.KeyGenerator = keyGenerator;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer BatchMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerBatchBuilder<T>(ProducerBatchMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the maximum number of messages to batch together.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public ProducerBatchBuilder<T> WithBatchSize(int batchSize)
    {
        options.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public ProducerBatchBuilder<T> WithBatchTimeout(TimeSpan timeout)
    {
        options.BatchTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer AwaitForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerAwaitForgetBuilder<T>(ProducerAwaitForgetMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the timeout duration for awaiting message processing.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder instance.</returns>
    public ProducerAwaitForgetBuilder<T> WithTimeout(TimeSpan timeout)
    {
        options.Timeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer FireForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerFireForgetBuilder<T>(ProducerFireForgetMiddlewareOptions<T> options) where T : class
{
    // ProducerFireForgetMiddlewareOptions<T> is currently empty, but we can add methods as needed
}
