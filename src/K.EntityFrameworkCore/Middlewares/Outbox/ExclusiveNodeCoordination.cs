using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// A no-op coordination strategy where a single worker processes all rows.
/// </summary>
internal sealed class ExclusiveNodeCoordination<TDbContext>(TDbContext context) : IOutboxCoordinationStrategy<TDbContext>
    where TDbContext : DbContext
{
    private readonly IProducer producer = context.GetInfrastructure().GetRequiredService<IProducer<string, byte[]>>();

    /*
     string topic = $"__{AppDomain.CurrentDomain.FriendlyName.ToLower()}.{typeof(TDbContext).Name.ToLower()}.poll";
        Message<string, byte[]> emptyMessage = new();

    //periodically produce to force poll.
        while (false) { }
        //await producer.ProduceAsync(topic, emptyMessage, cancellationToken);
     */

    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        return source;
    }
}
