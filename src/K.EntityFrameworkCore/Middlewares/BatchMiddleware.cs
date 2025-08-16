using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class BatchMiddleware<T>(BatchMiddlewareOptions<T> options) : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}
