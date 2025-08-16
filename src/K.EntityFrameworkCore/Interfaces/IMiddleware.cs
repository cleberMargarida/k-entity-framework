namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a middleware that processes messages of type <typeparamref name="T"/> asynchronously.
/// </summary>
public interface IMiddleware<T> where T : class
{
    /// <summary>
    /// Asynchronously invokes the middleware with the specified message.
    /// </summary>
    ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default);
}
