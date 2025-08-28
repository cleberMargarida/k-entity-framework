using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private readonly ConsumerMiddlewareInvoker<T> middleware;
        private readonly CancellationToken cancellationToken;

        /// <inheritdoc/>
        public T? Current { get; private set; }

        internal ConsumerEnumerator(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            middleware = serviceProvider.GetRequiredService<ConsumerMiddlewareInvoker<T>>();
            this.cancellationToken = cancellationToken;
        }

        /// <inheritdoc/>
        public async ValueTask<bool> MoveNextAsync()
        {
            do
            {
                var envelope = default(T).Seal();

                await middleware.InvokeAsync(envelope, cancellationToken);

                Current = envelope.Unseal();

            } while (Current == null && !cancellationToken.IsCancellationRequested);

            return !cancellationToken.IsCancellationRequested;
        }

        public ValueTask DisposeAsync()
        {
            // Middleware lifecycle is handled by the DI container
            return ValueTask.CompletedTask;
        }
    }
}

