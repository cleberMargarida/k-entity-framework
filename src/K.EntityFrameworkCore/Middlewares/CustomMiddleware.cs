using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class CustomMiddleware<T, TCustom>(TCustom middleware) : IMiddleware<T>
    where T : class
    where TCustom : IMiddleware<T>
{
    public async ValueTask InvokeAsync(IEnvelope<T> envelope, CancellationToken cancellationToken = default)
    {
        await middleware.InvokeAsync(envelope, cancellationToken);
    }
}
