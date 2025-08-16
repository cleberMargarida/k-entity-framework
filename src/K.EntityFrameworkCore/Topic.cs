using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Interfaces;

public class Topic<T>(DbContext dbContext) 
    : IProducer<T>
    , IConsumer<T>
    , IAsyncDisposable
    where T : class
{
    private readonly IServiceProvider serviceProvider = dbContext.GetInfrastructure();

    /// <summary>
    /// The current message for the particular partition offset.
    /// </summary>
    public T? Current { get; private set; }

    /// <inheritdoc/>
    public void Publish(in T domainEvent)
    {
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        return true;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
