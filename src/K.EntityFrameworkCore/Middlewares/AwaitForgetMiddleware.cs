using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal class AwaitForgetMiddleware<T>(AwaitForgetMiddlewareOptions<T> options) : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        try
        {
            await base.InvokeAsync(message, cancellationToken);
        }
        catch (Exception)
        {
            //forget
        }
    }
}
