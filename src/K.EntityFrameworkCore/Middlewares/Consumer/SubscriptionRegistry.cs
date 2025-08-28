using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace K.EntityFrameworkCore.Middlewares.Consumer
{
    internal sealed class SubscriptionRegistry<T>(IServiceProvider serviceProvider)
        where T : class
    {
#if NET9_0_OR_GREATER
        private static readonly Lock gate = new();
#else
        private static readonly object gate = new();
#endif

        private static int refCount;

        public IDisposable Activate()
        {
            // Resolve required settings lazily inside activation
            var settings = serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>();
            var clientSettings = serviceProvider.GetRequiredService<ClientSettings<T>>();
            var pollers = serviceProvider.GetRequiredService<IPollerManager>();

            // Choose shared or dedicated resources
            IConsumer consumer;
            if (settings.ExclusiveConnection)
            {
                pollers.EnsureDedicatedStarted(typeof(T));
                consumer = serviceProvider.GetRequiredKeyedService<IConsumer>(typeof(T));
            }
            else
            {
                pollers.EnsureSharedStarted();
                consumer = serviceProvider.GetRequiredService<IConsumer>();
            }

            // Subscribe or bump ref-count
            lock (gate)
            {
                if (refCount == 0)
                {
                    var assignments = consumer.Assignment.Select(tp => tp.Topic).ToHashSet();
                    assignments.Add(clientSettings.TopicName);
                    consumer.Subscribe(assignments);
                }
                refCount++;
            }

            return new DeactivationToken(serviceProvider, clientSettings.TopicName);
        }

        private sealed class DeactivationToken(IServiceProvider serviceProvider, string topic) : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                // We need to resolve settings again to choose the correct consumer
                var settings = serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>();

                IConsumer consumer;

                if (settings.ExclusiveConnection)
                {
                    consumer = serviceProvider.GetRequiredKeyedService<IConsumer>(typeof(T));
                }
                else
                {
                    consumer = serviceProvider.GetRequiredService<IConsumer>();
                }

                lock (gate)
                {
                    refCount = Math.Max(0, refCount - 1);

                    if (refCount > 0)
                    {
                        return;
                    }

                    var assignments = consumer.Assignment.Select(tp => tp.Topic).ToHashSet();
                    assignments.Remove(topic);
                    if (assignments.Count > 0)
                    {
                        consumer.Subscribe(assignments);
                    }
                    else
                    {
                        consumer.Unsubscribe();
                    }

                    // If using a dedicated poller for this type, stop it to release resources
                    if (settings.ExclusiveConnection)
                    {
                        serviceProvider.GetRequiredService<IPollerManager>().StopDedicated(typeof(T));
                    }
                }
            }
        }
    }
}
