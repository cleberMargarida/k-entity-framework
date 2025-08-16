    using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

[ScopedService]
internal abstract class Middleware<T>(MiddlewareOptions<T> options) : IMiddleware<T>
    where T : class
{
    protected Middleware() : this(new MiddlewareOptions<T>()) { }

    private readonly Stack<IMiddleware<T>> middlewareStack = new();

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the options.
    /// </summary>
    public bool IsEnabled => options.IsMiddlewareEnabled;

    protected void Use(IMiddleware<T> middleware)
    {
        middlewareStack.Push(middleware);
    }

    public virtual ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        // Only execute middleware if it's enabled
        if (!IsEnabled)
        {
            return ValueTask.CompletedTask;
        }

        return middlewareStack.Pop().InvokeAsync(message, cancellationToken);
    }
}
