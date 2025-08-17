namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a consumer that processes messages asynchronously.
/// </summary>
public interface IConsumer<out T>
    where T : class
{
    /// <summary>
    /// The current message for the particular partition offset.
    /// </summary>
    T? Current { get; }


    /// <summary>
    /// Asynchronously processes the next message in the topic-partition.
    /// </summary>
    ValueTask<bool> MoveNextAsync();
}