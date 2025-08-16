namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the BatchMiddleware.
/// </summary>
public class BatchMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages to batch together.
    /// Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum time to wait before processing a batch, even if it's not full.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether to process batches in parallel.
    /// Default is false.
    /// </summary>
    public bool ProcessInParallel { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism when ProcessInParallel is true.
    /// Default is Environment.ProcessorCount.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
