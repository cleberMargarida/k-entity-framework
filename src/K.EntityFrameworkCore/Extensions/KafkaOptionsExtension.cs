using Confluent.Kafka;
using K.EntityFrameworkCore.MiddlewareOptions;
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
            services.AddScoped(typeof(EventProcessingQueue));

            services.AddScoped(typeof(ConsumerMiddlewareInvoker<>));
            services.AddScoped(typeof(ProducerMiddlewareInvoker<>));

            services.AddSingleton(typeof(SerializationMiddlewareOptions<>));
            services.AddSingleton(typeof(ClientOptions<>));

            // Consumer-specific middleware options and classes
            services.AddScoped(typeof(DeserializationMiddleware<>));
            services.AddSingleton(typeof(ConsumerMiddlewareOptions<>));

            services.AddSingleton(typeof(ConsumerRetryMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerRetryMiddleware<>));

            services.AddSingleton(typeof(ConsumerCircuitBreakerMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerCircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(ConsumerThrottleMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerThrottleMiddleware<>));

            services.AddSingleton(typeof(ConsumerBatchMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerBatchMiddleware<>));

            services.AddSingleton(typeof(ConsumerForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerForgetMiddleware<>));

            // Producer-specific middleware options and classes
            services.AddSingleton(typeof(ProducerMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerMiddleware<>));

            services.AddScoped(typeof(SerializerMiddleware<>));

            services.AddSingleton(typeof(ProducerRetryMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerRetryMiddleware<>));

            services.AddSingleton(typeof(ProducerCircuitBreakerMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerCircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(ProducerThrottleMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerThrottleMiddleware<>));

            services.AddSingleton(typeof(ProducerBatchMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerBatchMiddleware<>));

            services.AddSingleton(typeof(ProducerForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerForgetMiddleware<>));

            services.AddSingleton(typeof(OutboxMiddlewareOptions<>));
            services.AddScoped(typeof(OutboxMiddleware<>));

            services.AddSingleton<Infrastructure<ClientConfig>>(_ => new(client));

            // One consumer per scope
            services.AddScoped<Infrastructure<IConsumer<Ignore, byte[]>>>(ConfluentConsumerFactory);

            // One producer per process
            services.AddSingleton<Infrastructure<IProducer<string, byte[]>>>(ConfluentProducerFactory);
        }

        private Infrastructure<IProducer<string, byte[]>> ConfluentProducerFactory(IServiceProvider provider)
        {
            var client = provider.GetRequiredService<Infrastructure<ClientConfig>>().Instance;
            return new Infrastructure<IProducer<string, byte[]>>(new ProducerBuilder<string, byte[]>(client).Build());

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
