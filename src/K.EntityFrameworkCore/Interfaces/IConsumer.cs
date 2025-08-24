namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a consumer that processes messages asynchronously.
/// </summary>
public interface IConsumer<out T> : IAsyncEnumerable<T>
    where T : class
{
}