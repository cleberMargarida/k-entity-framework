using System.Collections.Concurrent;
using K.EntityFrameworkCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer
{
    [SingletonService]
    internal sealed class PollerManager(IServiceProvider serviceProvider)
    {
        private readonly ConcurrentDictionary<Type, KafkaConsumerPollService> dedicated = new();

        public void EnsureSharedStarted()
        {
            serviceProvider.GetRequiredService<KafkaConsumerPollService>().EnsureStarted();
        }

        public void EnsureDedicatedStarted(Type type)
        {
            var poller = dedicated.GetOrAdd(type, static (t, sp) =>
                new KafkaConsumerPollService(sp, () => sp.GetRequiredKeyedService<IConsumer>(t)), serviceProvider);

            poller.EnsureStarted();
        }

        public void StopDedicated(Type type)
        {
            if (dedicated.TryRemove(type, out var poller))
            {
                try { poller.Stop(); } catch { }
                try { poller.Dispose(); } catch { }
            }
        }
    }
}
