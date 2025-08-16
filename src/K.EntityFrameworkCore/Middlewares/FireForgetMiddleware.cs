using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class FireForgetMiddleware<T>(FireForgetMiddlewareOptions<T> options) : Middleware<T>
    where T : class
{
    public override ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = base.InvokeAsync(message, cancellationToken).AsTask();
        }
        catch (Exception)
        {
            //forget
        }
        return ValueTask.CompletedTask;
    }
}
