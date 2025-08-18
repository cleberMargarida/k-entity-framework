using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a topic in the Kafka message broker that can be used for both producing and consuming messages.
/// </summary>
public partial class Topic<T>(DbContext dbContext)
    : IProducer<T>
    , IConsumer<T>
    where T : class
{
    private T? current;

    private readonly Lazy<ConsumerMiddlewareInvoker<T>> consumerMiddlewareInvoker = new(() => dbContext
        .GetInfrastructure()
        .GetRequiredService<ConsumerMiddlewareInvoker<T>>());

    /// <inheritdoc/>
    public T? Current => current;

    /// <inheritdoc/>
    public void Publish(T message)
    {
        dbContext.Publish(message);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        var envelope = SealEnvelop(null);
        await consumerMiddlewareInvoker.Value.InvokeAsync(envelope);
        return Unseal(envelope);
    }

    private bool Unseal(Envelope<T> envelope) => envelope.Message is not null && (current = envelope.Message) != null;

    private static Envelope<T> SealEnvelop(T? message) => new(message);
}

public static class DbContextExtensions
{
    /// <summary>
    /// Publishes a message of type <typeparamref name="T"/> to the event tracker associated with the DbContext.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <param name="message"></param>
    public static void Publish<T>(this DbContext dbContext, T message)
        where T : class
    {
        var serviceProvider = dbContext.GetInfrastructure();
        
        var eventProcessingQueue = serviceProvider.GetRequiredService<EventProcessingQueue>();
        
        eventProcessingQueue.Enqueue((IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        {
            var producerMiddlewareInvoker = serviceProvider.GetRequiredService<ProducerMiddlewareInvoker<T>>();
            return producerMiddlewareInvoker.InvokeAsync(new Envelope<T>(message), cancellationToken);
        });
    }
}

internal class EventProcessingQueue
{
    private readonly Queue<ServiceOperationDelegate> operations = new(3);

    internal void Enqueue(ServiceOperationDelegate operation)
    {
        operations.Enqueue(operation);
    }

    internal bool Dequeue([MaybeNullWhen(false)] out ServiceOperationDelegate operationDelegate)
    {
        return operations.TryDequeue(out operationDelegate);
    }
}

/// <summary>
/// Represents an asynchronous operation that uses dependency injection
/// and supports cancellation.
/// </summary>
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>A task-like value representing the asynchronous operation.</returns>
public delegate ValueTask ServiceOperationDelegate(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);
