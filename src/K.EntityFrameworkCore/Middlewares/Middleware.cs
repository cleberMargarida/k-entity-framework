using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

[ScopedService]
internal abstract class Middleware<T>(MiddlewareOptions<T> options) : IMiddleware<T>
    where T : class
{
    private IMiddleware<T>? next;

    protected Middleware() : this(new MiddlewareOptions<T>()) { }

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the options.
    /// </summary>
    public bool IsEnabled => options.IsMiddlewareEnabled;

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
