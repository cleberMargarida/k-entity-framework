using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal class CustomMiddleware<T, TCustom>(TCustom middleware) : IMiddleware<T>
    where T : class
    where TCustom : IMiddleware<T>
{
    public async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await middleware.InvokeAsync(message, cancellationToken);
    }
}
