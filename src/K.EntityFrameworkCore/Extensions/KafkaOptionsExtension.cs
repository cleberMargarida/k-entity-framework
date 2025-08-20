global using IProducer = Confluent.Kafka.IProducer<string, byte[]>;
global using IBatchProducer = Confluent.Kafka.IProducer<string, byte[]>;

using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using K.EntityFrameworkCore.Middlewares.Batch;
using K.EntityFrameworkCore.Middlewares.CircuitBreaker;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Retry;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using K.EntityFrameworkCore.Middlewares.Core;


namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaOptionsExtension : IDbContextOptionsExtension
    {
        // TODO dictionary of context types to options
        internal static IDbContextOptions? CachedOptions;

        private readonly ClientConfig client;
        private readonly Type contextType;

        public KafkaOptionsExtension(ClientConfig client, Type contextType)
        {
            this.client = client;
            this.contextType = contextType;
            Info = new KafkaOptionsExtensionInfo(this);
        }

        public DbContextOptionsExtensionInfo Info { get; }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddScoped<ScopedCommandRegistry>();

            services.AddScoped(typeof(ConsumerMiddlewareInvoker<>));
            services.AddScoped(typeof(ProducerMiddlewareInvoker<>));

            services.AddSingleton(typeof(SerializationMiddlewareSettings<>));
            services.AddSingleton(typeof(ClientSettings<>));

            // Consumer-specific middleware options and classes
            services.AddScoped(typeof(DeserializerMiddleware<>));
            services.AddSingleton(typeof(ConsumerMiddlewareSettings<>));

            services.AddSingleton(typeof(ConsumerRetryMiddlewareSettings<>));
            services.AddScoped(typeof(ConsumerRetryMiddleware<>));

            services.AddSingleton(typeof(ConsumerCircuitBreakerMiddlewareSettings<>));
            services.AddScoped(typeof(ConsumerCircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(ConsumerBatchMiddlewareSettings<>));
            services.AddScoped(typeof(ConsumerBatchMiddleware<>));

            services.AddSingleton(typeof(ConsumerForgetMiddlewareSettings<>));
            services.AddScoped(typeof(ConsumerForgetMiddleware<>));

            // Producer-specific middleware options and classes
            services.AddSingleton(typeof(ProducerMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerMiddleware<>));

            services.AddScoped(typeof(SerializerMiddleware<>));

            services.AddSingleton(typeof(ProducerRetryMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerRetryMiddleware<>));

            services.AddSingleton(typeof(ProducerCircuitBreakerMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerCircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(ProducerBatchMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerBatchMiddleware<>));

            services.AddSingleton(typeof(ProducerForgetMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerForgetMiddleware<>));

            services.AddSingleton(typeof(OutboxMiddlewareSettings<>));
            services.AddScoped(typeof(OutboxMiddleware<>));

            services.AddSingleton<ClientConfig>(_ => new(client));

            // One consumer per scope
            services.AddScoped<IConsumer<Ignore, byte[]>>(ConsumerFactory);

            // One producer per process
            services.AddSingleton<IProducer>(ProducerFactory);

            //TODO: move to source generator is okay reflection here?
            foreach (var item in contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => 
                    prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(Topic<>)))
                .Select(prop => prop.PropertyType.GenericTypeArguments[0]))
            {
                services.AddKeyedSingleton<IBatchProducer>(item, ProducerFactory);
            }
        }

        private IProducer ProducerFactory(IServiceProvider provider, object? key)
        {
            Type type = (Type)key!;

            var batchSettings = (IBatchMiddlewareSettings)provider.GetRequiredService(typeof(ProducerBatchMiddlewareSettings<>).MakeGenericType(type));

            var client = provider.GetRequiredService<ClientConfig>();
            var producerConfig = new ProducerConfig(client)
            {
                BatchSize = batchSettings.BatchSize,
                MessageTimeoutMs = batchSettings.BatchTimeoutMilliseconds,
            };

            return ProducerFactory(producerConfig);
        }

        private IProducer ProducerFactory(IServiceProvider provider)
        {
            var client = provider.GetRequiredService<ClientConfig>();
            var producerConfig = new ProducerConfig(client);
            return ProducerFactory(producerConfig);
        }

        private static IProducer ProducerFactory(ClientConfig client)
        {
            return new ProducerBuilder<string, byte[]>(client)
                .SetLogHandler((_,_) => { }).Build();//TODO handle kafka logs
        }

        private IConsumer<Ignore, byte[]> ConsumerFactory(IServiceProvider provider)
        {
            var client = provider.GetRequiredService<ClientConfig>();
            return new ConsumerBuilder<Ignore, byte[]>(client).Build();
        }

        public void Validate(IDbContextOptions options)
        {
            CachedOptions = options;
        }
    }
}
