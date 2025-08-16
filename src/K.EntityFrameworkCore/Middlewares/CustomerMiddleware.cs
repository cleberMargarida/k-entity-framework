using K.EntityFrameworkCore.Middlewares.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class CustomerMiddleware<T, TCustom>(TCustom middleware) : Middleware<T>
    where T : class
    where TCustom : IMiddleware<T>
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await middleware.InvokeAsync(message, cancellationToken);
        await base.InvokeAsync(message, cancellationToken);
    }
}
