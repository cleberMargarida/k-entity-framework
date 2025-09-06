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
    ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the settings.
    /// </summary>
    bool IsEnabled => false;

    /// <summary>
    /// Gets a value indicating whether this middleware is disabled based on the settings.
    /// </summary>
    bool IsDisabled => !IsEnabled;

    internal IMiddleware<T>? Next { get; set; }
}
