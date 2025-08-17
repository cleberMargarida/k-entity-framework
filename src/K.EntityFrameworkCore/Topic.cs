using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares;

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

    private readonly Lazy<ProducerMiddlewareInvoker<T>> producerMiddlewareInvoker = new(() => dbContext
        .GetInfrastructure()
        .GetRequiredService<ProducerMiddlewareInvoker<T>>());

    /// <inheritdoc/>
    public T? Current => current;

    /// <inheritdoc/>
    public void Publish(in T domainEvent)
    {
        var message = domainEvent;
        var envelope = SealEnvelop(message);
        producerMiddlewareInvoker.Value.InvokeAsync(envelope).AsTask();
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
