using Microsoft.EntityFrameworkCore;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Defines the contract for scoping outbox queries to the subset of rows
/// this worker instance is allowed to process. Implementations must return
/// an expression that EF Core can translate to SQL so rows are filtered at the database.
/// </summary>
public interface IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Returns a SQL-translatable query filter that limits the outbox rows
    /// to those owned by this worker instance. This prevents loading and discarding rows.
    /// </summary>
    /// <param name="source">The base outbox query.</param>
    /// <returns>The filtered query that this worker should process.</returns>
    IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source);
}
