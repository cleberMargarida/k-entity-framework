namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a consumer that processes messages asynchronously.
/// </summary>
public interface IConsumer<out T> : IAsyncEnumerator<T?>
    where T : class
{
    /// <summary>
    /// Asynchronously processes the next message in the topic-partition.
    /// </summary>
    new ValueTask<bool> MoveNextAsync();
}