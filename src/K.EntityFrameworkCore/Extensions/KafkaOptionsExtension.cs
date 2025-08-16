using Confluent.Kafka;
using K.EntityFrameworkCore.MiddlewareOptions.Consumer;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;
using K.EntityFrameworkCore.Middlewares;
using K.EntityFrameworkCore.Middlewares.Consumer;
using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaOptionsExtension : IDbContextOptionsExtension
    {
        internal static IDbContextOptions? CachedOptions;

        private readonly ClientConfig client;

        public KafkaOptionsExtension(ClientConfig client)
        {
            this.client = client;
            Info = new KafkaOptionsExtensionInfo(this);
        }

        public DbContextOptionsExtensionInfo Info { get; }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddScoped(typeof(ConsumerMiddlewareInvoker<>));
            services.AddScoped(typeof(ProducerMiddlewareInvoker<>));

            // Consumer-specific middleware options and classes
            services.AddSingleton(typeof(ConsumerRetryMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerRetryMiddleware<>));

            services.AddSingleton(typeof(ConsumerCircuitBreakerMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerCircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(ConsumerThrottleMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerThrottleMiddleware<>));

            services.AddSingleton(typeof(ConsumerBatchMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerBatchMiddleware<>));

            services.AddSingleton(typeof(ConsumerAwaitForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerAwaitForgetMiddleware<>));

            services.AddSingleton(typeof(ConsumerFireForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerFireForgetMiddleware<>));

            // Producer-specific middleware options and classes
            services.AddSingleton(typeof(ProducerRetryMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerRetryMiddleware<>));

            services.AddSingleton(typeof(ProducerCircuitBreakerMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerCircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(ProducerThrottleMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerThrottleMiddleware<>));

            services.AddSingleton(typeof(ProducerBatchMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerBatchMiddleware<>));

            services.AddSingleton(typeof(ProducerAwaitForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerAwaitForgetMiddleware<>));

            services.AddSingleton(typeof(ProducerFireForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerFireForgetMiddleware<>));

            services.AddSingleton<Infrastructure<ClientConfig>>(_ => new(client));
            services.AddSingleton<Infrastructure<IProducer<byte[], byte[]>>>(ConfluentProducerFactory);
            services.AddSingleton<Infrastructure<IConsumer<Ignore, byte[]>>>(ConfluentConsumerFactory);
        }

        private Infrastructure<IProducer<byte[], byte[]>> ConfluentProducerFactory(IServiceProvider provider)
        {
            var client = provider.GetRequiredService<Infrastructure<ClientConfig>>().Instance;
            return new Infrastructure<IProducer<byte[], byte[]>>(new ProducerBuilder<byte[], byte[]>(client).Build());

        }

        private Infrastructure<IConsumer<Ignore, byte[]>> ConfluentConsumerFactory(IServiceProvider provider)
        {
            var client = provider.GetRequiredService<Infrastructure<ClientConfig>>().Instance;
            return new Infrastructure<IConsumer<Ignore, byte[]>>(new ConsumerBuilder<Ignore, byte[]>(client).Build());
        }

        public void Validate(IDbContextOptions options)
        {
            CachedOptions = options;
        }
    }
}
