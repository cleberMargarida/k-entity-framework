using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

internal class InboxMiddleware<T>(InboxMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return base.InvokeAsync(envelope, cancellationToken);
    }
}
