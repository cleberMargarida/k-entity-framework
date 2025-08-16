using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares;
using System.Collections.Generic;

/// <summary>
/// Represents a topic in the Kafka message broker that can be used for both producing and consuming messages.
/// </summary>
public class Topic<T>(DbContext dbContext)
    : IProducer<T>
    , IConsumer<T>
    , IAsyncDisposable
    where T : class
{
    private T? current;

    private readonly Lazy<ConsumerMiddlewareInvoker<T>> consumerMiddlewareInvoker = new(() => dbContext
        .GetInfrastructure()
        .GetRequiredService<ConsumerMiddlewareInvoker<T>>());

    private readonly Lazy<ProducerMiddlewareInvoker<T>> producerMiddlewareInvoker = new(() => dbContext
        .GetInfrastructure()
        .GetRequiredService<ProducerMiddlewareInvoker<T>>());

    /// <summary>
    /// The current message for the particular partition offset.
    /// </summary>
    public T? Current => current;

    /// <inheritdoc/>
    public void Publish(in T domainEvent)
    {
        var envelope = SealEnvelop(ref domainEvent);
        producerMiddlewareInvoker.Value.InvokeAsync(envelope).AsTask();
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        T? message = null;
        var envelope = SealEnvelop(ref message);
        await consumerMiddlewareInvoker.Value.InvokeAsync(envelope);
        return Unseal(envelope);
    }

    private bool Unseal(Envelope envelope)
    {
        return envelope.Message is not null && (current = envelope.Message) != null;
    }

    private static Envelope SealEnvelop(ref T? message)
    {
        return new(ref message);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    internal ref struct Envelope(ref T? message) : IEnvelope<T>, ISerializedEnvelope<T>
    {
        private ref T? message = ref message;

        public Dictionary<string, object>? Headers { get; set; }

        public byte[]? SerializedData { get; set; }

        public T? Message 
        { 
            readonly get => message; 
            set => message = value; 
        }
    }
}
