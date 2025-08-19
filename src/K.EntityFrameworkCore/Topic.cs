using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Represents a topic in the Kafka message broker that can be used for both producing and consuming messages.
/// </summary>
public sealed class Topic<T>(DbContext dbContext)
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
        var envelope = Seal(default(T));
        await consumerMiddlewareInvoker.Value.InvokeAsync(envelope);
        return Unseal(envelope);
    }

    private static Envelope<T> Seal(T? message)
    {
        return EnvelopeExtensions.Seal(message);
    }

    private bool Unseal(Envelope<T> envelope)
    {
        return envelope.HasMessage() && (current = envelope.Unseal()) != null;
    }
}

