using K.EntityFrameworkCore.Middlewares;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring OutboxMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxBuilder<T>(OutboxMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Configures immediate publishing strategy with fallback to background processing.
    /// Messages are published immediately after saving. If successful, they are removed.
    /// If immediate publishing fails, messages fall back to background processing.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseImmediateWithFallback()
    {
        settings.Strategy = OutboxPublishingStrategy.ImmediateWithFallback;
        return this;
    }

    /// <summary>
    /// Configures background-only publishing strategy.
    /// Messages are always published in the background after saving.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseBackgroundOnly()
    {
        settings.Strategy = OutboxPublishingStrategy.BackgroundOnly;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer RetryMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerRetryBuilder<T>(ProducerRetryMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseMaxAttempts(int maxAttempts)
    {
        settings.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the base delay between retry attempts.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseBaseDelay(TimeSpan delay)
    {
        settings.BaseDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retry attempts.
    /// </summary>
    /// <param name="maxDelay">The maximum delay.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseMaxDelay(TimeSpan maxDelay)
    {
        settings.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff strategy for retry delays.
    /// </summary>
    /// <param name="strategy">The backoff strategy.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseBackoffStrategy(RetryBackoffStrategy strategy)
    {
        settings.BackoffStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff.
    /// </summary>
    /// <param name="multiplier">The backoff multiplier.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseBackoffMultiplier(double multiplier)
    {
        settings.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter to avoid thundering herd.
    /// </summary>
    /// <param name="useJitter">Whether to use jitter.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseJitter(bool useJitter = true)
    {
        settings.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Sets exception types that should trigger a retry.
    /// </summary>
    /// <param name="exceptionTypes">The exception types to retry on.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetriableExceptions(params Type[] exceptionTypes)
    {
        settings.RetriableExceptionTypes = exceptionTypes;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate to determine if an exception should trigger a retry.
    /// </summary>
    /// <param name="predicate">The retry predicate.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetryPredicate(Func<Exception, bool> predicate)
    {
        settings.ShouldRetryPredicate = predicate;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer CircuitBreakerMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerCircuitBreakerBuilder<T>(ProducerCircuitBreakerMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <param name="threshold">The failure threshold.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseFailureThreshold(int threshold)
    {
        settings.FailureThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the time to wait before attempting to reset the circuit breaker.
    /// </summary>
    /// <param name="timeout">The open timeout.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseOpenTimeout(TimeSpan timeout)
    {
        settings.OpenTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the time window for counting failures.
    /// </summary>
    /// <param name="duration">The sampling duration.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseSamplingDuration(TimeSpan duration)
    {
        settings.SamplingDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of requests required before the circuit breaker can trip.
    /// </summary>
    /// <param name="throughput">The minimum throughput.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseMinimumThroughput(int throughput)
    {
        settings.MinimumThroughput = throughput;
        return this;
    }

    /// <summary>
    /// Sets exception types that should count as failures.
    /// </summary>
    /// <param name="exceptionTypes">The exception types that trigger circuit breaking.</param>
    /// <returns>The builder instance.</returns>
    public ProducerCircuitBreakerBuilder<T> UseBreakOnExceptions(params Type[] exceptionTypes)
    {
        settings.ExceptionTypesToBreakOn = exceptionTypes;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer BatchMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerBatchBuilder<T>(ProducerBatchMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the maximum number of messages to batch together.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public ProducerBatchBuilder<T> UseBatchSize(int batchSize)
    {
        settings.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public ProducerBatchBuilder<T> UseBatchTimeout(TimeSpan timeout)
    {
        settings.BatchTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring producer ForgetMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerForgetBuilder<T>(ProducerForgetMiddlewareSettings<T> settings) where T : class
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
        settings.Strategy = ForgetStrategy.AwaitForget;
        settings.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        return this;
    }

    /// <summary>
    /// Sets the forget strategy to FireForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ProducerForgetBuilder<T> UseFireForget()
    {
        settings.Strategy = ForgetStrategy.FireForget;
        return this;
    }
}
