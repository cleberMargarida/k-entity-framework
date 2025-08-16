using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

[ScopedService]
internal class Middleware<T> : IMiddlewareConfig<T>, IMiddleware<T>
    where T : class
{
    private readonly Stack<IMiddleware<T>> middlewareStack = new();

    void IMiddlewareConfig<T>.Use(IMiddleware<T> middleware)
    {
        middlewareStack.Push(middleware);
    }

    public virtual ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        return middlewareStack.Pop().InvokeAsync(message, cancellationToken);
    }
}
