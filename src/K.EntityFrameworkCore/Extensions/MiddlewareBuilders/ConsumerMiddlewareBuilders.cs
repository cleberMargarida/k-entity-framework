using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring InboxMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxBuilder<T> where T : class
{
    private readonly InboxMiddlewareOptions<T> _options;

    internal InboxBuilder(InboxMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the timeout for duplicate message detection.
    /// </summary>
    /// <param name="timeout">The duplicate detection timeout.</param>
    /// <returns>The builder instance.</returns>
    public InboxBuilder<T> WithDuplicateDetectionTimeout(TimeSpan timeout)
    {
        _options.DuplicateDetectionTimeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring BatchMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class BatchBuilder<T> where T : class
{
    private readonly BatchMiddlewareOptions<T> _options;

    internal BatchBuilder(BatchMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the maximum number of messages to batch together.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public BatchBuilder<T> WithBatchSize(int batchSize)
    {
        _options.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the maximum time to wait before processing a batch.
    /// </summary>
    /// <param name="timeout">The batch timeout.</param>
    /// <returns>The builder instance.</returns>
    public BatchBuilder<T> WithBatchTimeout(TimeSpan timeout)
    {
        _options.BatchTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables parallel processing of batches.
    /// </summary>
    /// <param name="processInParallel">Whether to process in parallel.</param>
    /// <returns>The builder instance.</returns>
    public BatchBuilder<T> WithParallelProcessing(bool processInParallel = true)
    {
        _options.ProcessInParallel = processInParallel;
        return this;
    }

    /// <summary>
    /// Sets the maximum degree of parallelism when parallel processing is enabled.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
    /// <returns>The builder instance.</returns>
    public BatchBuilder<T> WithMaxDegreeOfParallelism(int maxDegreeOfParallelism)
    {
        _options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring AwaitForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class AwaitForgetBuilder<T> where T : class
{
    private readonly AwaitForgetMiddlewareOptions<T> _options;

    internal AwaitForgetBuilder(AwaitForgetMiddlewareOptions<T> options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets the timeout duration for awaiting message processing.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder instance.</returns>
    public AwaitForgetBuilder<T> WithTimeout(TimeSpan timeout)
    {
        _options.Timeout = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring FireForgetMiddleware options.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class FireForgetBuilder<T> where T : class
{
    private readonly FireForgetMiddlewareOptions<T> _options;

    internal FireForgetBuilder(FireForgetMiddlewareOptions<T> options)
    {
        _options = options;
    }

    // FireForgetMiddlewareOptions<T> is currently empty, but we can add methods as needed
}
