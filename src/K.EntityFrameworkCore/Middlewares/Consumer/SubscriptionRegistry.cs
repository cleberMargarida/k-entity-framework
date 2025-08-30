using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer
{
    [ScopedService]
    internal sealed class SubscriptionRegistry<T>(IServiceProvider serviceProvider)
        where T : class
    {
#if NET9_0_OR_GREATER
        private readonly Lock gate = new();
#else
        private readonly object gate = new();
#endif

        public IDisposable Activate()
        {
            // Resolve required settings lazily inside activation
            var settings = serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>();
            var clientSettings = serviceProvider.GetRequiredService<ClientSettings<T>>();
            var pollers = serviceProvider.GetRequiredService<PollerManager>();

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
                var assignments = consumer.Assignment.Select(tp => tp.Topic).ToHashSet();
                assignments.Add(clientSettings.TopicName);
                consumer.Subscribe(assignments);
            }

            return new DeactivationToken(consumer, pollers, gate, clientSettings.TopicName, settings.ExclusiveConnection, typeof(T));
        }

#if NET9_0_OR_GREATER
        private sealed class DeactivationToken(IConsumer consumer, PollerManager pollerManager, Lock gate, string topic, bool exclusiveConnection, Type consumerType) : IDisposable
#else
        private sealed class DeactivationToken(IConsumer consumer, PollerManager pollerManager, object gate, string topic, bool exclusiveConnection, Type consumerType) : IDisposable
#endif
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                lock (gate)
                {
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
                    if (exclusiveConnection)
                    {
                        pollerManager.StopDedicated(consumerType);
                    }
                }
            }
        }
    }
}
