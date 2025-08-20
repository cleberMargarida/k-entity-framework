using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Core;

[ScopedService]
internal abstract class Middleware<T>(MiddlewareSettings<T> settings) : IMiddleware<T>
    where T : class
{
    private IMiddleware<T>? next;

    protected Middleware() : this(new MiddlewareSettings<T>()) { }

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the settings.
    /// </summary>
    public bool IsEnabled => settings.IsMiddlewareEnabled;

    IMiddleware<T>? IMiddleware<T>.Next
    {
        get => next;
        set => next = value;
    }

    public virtual ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return next?.InvokeAsync(envelope, cancellationToken) ?? ValueTask.CompletedTask;
    }
}
