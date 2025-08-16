using K.EntityFrameworkCore.Middlewares.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class FireForgetMiddleware<T> : Middleware<T>
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
