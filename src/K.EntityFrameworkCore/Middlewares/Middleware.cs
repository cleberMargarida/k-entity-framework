    using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using System.Threading;

namespace K.EntityFrameworkCore.Middlewares;

[ScopedService]
internal abstract class Middleware<T>(MiddlewareOptions<T> options) : IMiddleware<T>
    where T : class
{
    protected Middleware() : this(new MiddlewareOptions<T>()) { }

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the options.
    /// </summary>
    public bool IsEnabled => options.IsMiddlewareEnabled;

    public virtual ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
