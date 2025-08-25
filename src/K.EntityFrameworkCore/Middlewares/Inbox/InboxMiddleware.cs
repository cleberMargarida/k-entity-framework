using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

internal class InboxMiddleware<T>(ICurrentDbContext currentDbContext, InboxMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    private readonly DbContext context = currentDbContext.Context;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        context.Set<InboxMessage>().Add(envelope.Message);

        return base.InvokeAsync(envelope, cancellationToken);
    }
}
