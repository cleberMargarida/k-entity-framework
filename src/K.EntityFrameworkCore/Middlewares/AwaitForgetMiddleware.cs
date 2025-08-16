using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class AwaitForgetMiddleware<T> : Middleware<T>
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
