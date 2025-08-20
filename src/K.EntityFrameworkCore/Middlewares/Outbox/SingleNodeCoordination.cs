using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// A no-op coordination strategy where a single worker processes all rows.
/// </summary>
internal sealed class SingleNodeCoordination<TDbContext> : IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        return source;
    }
}
