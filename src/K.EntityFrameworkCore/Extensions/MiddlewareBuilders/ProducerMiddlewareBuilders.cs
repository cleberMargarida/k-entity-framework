using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;

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
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseMaxRetries(int maxRetries)
    {
        settings.MaxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the base backoff time in milliseconds before retrying.
    /// </summary>
    /// <param name="milliseconds">The backoff time in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetryBackoffMilliseconds(int milliseconds)
    {
        settings.RetryBackoffMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the base backoff time using a TimeSpan.
    /// </summary>
    /// <param name="delay">The backoff time as a TimeSpan.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetryBackoff(TimeSpan delay)
    {
        settings.RetryBackoffMilliseconds = (int)delay.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum backoff time in milliseconds before retrying.
    /// </summary>
    /// <param name="milliseconds">The maximum backoff time in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetryBackoffMaxMilliseconds(int milliseconds)
    {
        settings.RetryBackoffMaxMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum backoff time using a TimeSpan.
    /// </summary>
    /// <param name="maxDelay">The maximum backoff time as a TimeSpan.</param>
    /// <returns>The builder instance.</returns>
    public ProducerRetryBuilder<T> UseRetryBackoffMax(TimeSpan maxDelay)
    {
        settings.RetryBackoffMaxMilliseconds = (int)maxDelay.TotalMilliseconds;
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
