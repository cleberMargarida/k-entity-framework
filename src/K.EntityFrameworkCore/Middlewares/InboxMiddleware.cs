using K.EntityFrameworkCore.Middlewares.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class InboxMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}
