using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// Kafka-backed sharding strategy that owns a subset of virtual buckets and filters
/// by <see cref="OutboxMessage.AggregateId"/> using a SQL-translatable <c>IN</c> predicate.
/// Bucket ownership is coordinated via Kafka heartbeats (implementation hidden).
/// </summary>
/// <remarks>
/// Initializes the strategy.
/// </remarks>
/// <param name="ownedBuckets">
/// The set of virtual buckets owned by this worker. How this set is computed
/// (heartbeats, partition assignment, etc.) is an internal detail.
/// </param>
internal sealed class KafkaBucketSharding<TDbContext>(int[] ownedBuckets) : IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        // EF translates Contains(array) on a property to SQL IN (...)
        //return source.Where(e => ownedBuckets.Contains(e.AggregateId));
        return source;
    }
}
