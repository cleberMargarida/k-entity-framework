using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Represents a topic in the Kafka message broker that can be used for both producing and consuming messages.
/// </summary>
public sealed class Topic<T>(DbContext context)
    : IProducer<T>
    , IConsumer<T>
    where T : class
{
    /// <inheritdoc/>
    public void Publish(T message) 
        => context.Publish(message);

    /// <inheritdoc/>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) 
        => new ConsumerEnumerator(context.GetInfrastructure(), cancellationToken)!;

    private class ConsumerEnumerator : IAsyncEnumerator<T?>
    {
        private readonly SubscriptionHandler<T> subscriptionHandle;
        private readonly ConsumerMiddlewareInvoker<T> middleware;
        private readonly CancellationToken cancellationToken;

        /// <inheritdoc/>
        public T? Current { get; private set; }

        internal ConsumerEnumerator(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            subscriptionHandle = serviceProvider.GetRequiredService<SubscriptionHandler<T>>();
            subscriptionHandle.Subscribe();
            middleware = serviceProvider.GetRequiredService<ConsumerMiddlewareInvoker<T>>();
            this.cancellationToken = cancellationToken;
        }

        /// <inheritdoc/>
        public async ValueTask<bool> MoveNextAsync()
        {
            var envelope = default(T).Seal();
            await middleware.InvokeAsync(envelope, this.cancellationToken);
            return Unseal(envelope);
        }

        private bool Unseal(Envelope<T> envelope)
        {
            return envelope.HasMessage() && (Current = envelope.Unseal()) != null;
        }

        public ValueTask DisposeAsync()
        {
            subscriptionHandle.Unsubscribe();
            return ValueTask.CompletedTask;
        }
    }
}

