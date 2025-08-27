using Confluent.Kafka;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Interface for providing consume results from Kafka through async channels.
/// Replaces the synchronous TaskCompletionSource-based approach with bounded channels
/// to prevent blocking and provide better backpressure handling.
/// </summary>
internal interface IConsumeResultChannel
{
    /// <summary>
    /// Attempts to enqueue a consume result to the channel.
    /// Returns true if the result was successfully enqueued, false if the channel is full.
    /// </summary>
    /// <param name="result">The consume result to enqueue</param>
    /// <returns>True if enqueued successfully, false if channel is full</returns>
    bool TryEnqueue(ConsumeResult<string, byte[]> result);

    /// <summary>
    /// Writes a consume result to the channel asynchronously.
    /// </summary>
    /// <param name="result">The consume result to write</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A task representing the asynchronous write operation</returns>
    ValueTask WriteAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the next consume result from the channel asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The next consume result</returns>
    ValueTask<ConsumeResult<string, byte[]>> ReadAsync(CancellationToken cancellationToken = default);
}
