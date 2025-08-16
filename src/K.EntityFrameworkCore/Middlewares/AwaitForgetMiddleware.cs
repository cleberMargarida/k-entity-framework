using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class AwaitForgetMiddleware<T>(AwaitForgetMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> envelope, CancellationToken cancellationToken = default)
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
