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
    /// Configures immediate publishing strategy with fallback to background processing.
    /// Messages are published immediately after saving. If successful, they are removed.
    /// If immediate publishing fails, messages fall back to background processing.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseImmediateWithFallback()
    {
        options.Strategy = OutboxPublishingStrategy.ImmediateWithFallback;
        return this;
    }

    /// <summary>
    /// Configures background-only publishing strategy.
    /// Messages are always published in the background after saving.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseBackgroundOnly()
    {
        options.Strategy = OutboxPublishingStrategy.BackgroundOnly;
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
    public ProducerRetryBuilder<T> UseMaxAttempts(int maxAttempts)
    {
        options.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseBaseDelay(TimeSpan delay)
    {
        options.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseMaxDelay(TimeSpan maxDelay)
    {
        options.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseBackoffStrategy(RetryBackoffStrategy strategy)
    {
        options.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseBackoffMultiplier(double multiplier)
    {
        options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseJitter(bool useJitter = true)
    {
        options.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetriableExceptions(params Type[] exceptionTypes)
    {
        options.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetryPredicate(Func<Exception, bool> predicate)
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
    public ProducerCircuitBreakerBuilder<T> UseFailureThreshold(int threshold)
    {
        options.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseOpenTimeout(TimeSpan timeout)
    {
        options.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseSamplingDuration(TimeSpan duration)
    {
        options.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseMinimumThroughput(int throughput)
    {
        options.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseBreakOnExceptions(params Type[] exceptionTypes)
    {
        options.ExceptionTypesToBreakOn = exceptionTypes;
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
    public ProducerBatchBuilder<T> UseBatchSize(int batchSize)
    {
        options.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public ProducerBatchBuilder<T> UseBatchTimeout(TimeSpan timeout)
    {
        options.BatchTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer ForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerForgetBuilder<T>(ProducerForgetMiddlewareOptions<T> options) where T : class
{
    /// <summary>
    /// Sets the forget strategy to AwaitForget.
    /// </summary>
    /// <param name="timeout">
    /// The timeout duration for awaiting message processing.
    /// </param>
    /// <returns>The builder instance.</returns>
    public ProducerForgetBuilder<T> UseAwaitForget(TimeSpan? timeout = null)
    {
        options.Strategy = ForgetStrategy.AwaitForget;
        options.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        return this;
    }

    /// <summary>
    /// Sets the forget strategy to FireForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ProducerForgetBuilder<T> UseFireForget()
    {
        options.Strategy = ForgetStrategy.FireForget;
        return this;
    }
}
