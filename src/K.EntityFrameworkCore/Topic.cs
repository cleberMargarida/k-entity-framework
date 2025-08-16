using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore;

public class Topic<T>(DbContext dbContext) : IAsyncEnumerator<T?>, IAsyncDisposable
    where T : class
{
    private readonly IServiceProvider serviceProvider = dbContext.GetInfrastructure();

    /// <summary>
    /// The current message for the particular partition offset.
    /// </summary>
    public T? Current { get; private set; }

    /// <summary>
    /// Publishes a domain event to the Kafka topic.
    /// </summary>
    public void Publish(in T domainEvent)
    {
    }

    /// <summary>
    /// Moves the enumerator to the next message in the topic partition.
    /// </summary>
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
