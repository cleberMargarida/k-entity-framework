using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

internal class InboxMiddleware<T>(ICurrentDbContext currentDbContext, InboxMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    private readonly DbContext context = currentDbContext.Context;

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        ulong hashId = settings.Hash(envelope);

        DbSet<InboxMessage> inboxMessages = context.Set<InboxMessage>();

        var isDuplicate = (await inboxMessages.FindAsync(new object[] { hashId }, cancellationToken)) != null;
        if (isDuplicate)
        {
            envelope.Clean();
            return;
        }

        inboxMessages.Add(new()
        {
            HashId = hashId,
            ReceivedAt = DateTime.UtcNow,
        });

        await base.InvokeAsync(envelope, cancellationToken);
    }
}
