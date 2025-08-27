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
    /// Writes a consume result to the channel asynchronously.
    /// </summary>
    /// <param name="result">The consume result to write</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>A task representing the asynchronous write operation</returns>
    ValueTask WriteAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellationToken = default);
}
