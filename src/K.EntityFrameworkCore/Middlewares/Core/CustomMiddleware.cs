using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Core;

/// <summary>
/// Internal adapter that wraps a user-provided <see cref="IMiddleware{T}"/> implementation
/// and integrates it into the middleware pipeline chain.
/// The wrapper forwards <see cref="IMiddleware{T}.IsEnabled"/> to the inner middleware
/// and delegates <see cref="IMiddleware{T}.InvokeAsync"/> before continuing the chain
/// via <see cref="Middleware{T}.InvokeAsync"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class CustomMiddleware<T>(IMiddleware<T> inner)
    : Middleware<T>(new MiddlewareSettings<T>(isMiddlewareEnabled: inner.IsEnabled))
    where T : class
{
    /// <inheritdoc />
    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        var innerTask = inner.InvokeAsync(envelope, cancellationToken);
        if (!innerTask.IsCompletedSuccessfully)
            innerTask.GetAwaiter().GetResult();
        return base.InvokeAsync(envelope, cancellationToken);
    }
}
