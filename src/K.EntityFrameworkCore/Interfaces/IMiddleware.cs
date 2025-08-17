using Microsoft.Extensions.Options;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a middleware that processes messages of type <typeparamref name="T"/> asynchronously.
/// </summary>
public interface IMiddleware<T> where T : class
{
    /// <summary>
    /// Asynchronously invokes the middleware with the specified message.
    /// </summary>
    ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the options.
    /// </summary>
    bool IsEnabled => false;
}
