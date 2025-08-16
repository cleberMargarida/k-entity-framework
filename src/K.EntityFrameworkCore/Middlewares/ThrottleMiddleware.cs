using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class ThrottleMiddleware<T>(ThrottleMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> envelope, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}
