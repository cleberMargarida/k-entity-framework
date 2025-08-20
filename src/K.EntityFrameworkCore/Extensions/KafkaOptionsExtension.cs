global using IProducer = Confluent.Kafka.IProducer<string, byte[]>;
global using IBatchProducer = Confluent.Kafka.IProducer<string, byte[]>;

using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Consumer;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;
using K.EntityFrameworkCore.Middlewares;
using K.EntityFrameworkCore.Middlewares.Consumer;
using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


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

            services.AddSingleton(typeof(SerializationMiddlewareOptions<>));
            services.AddSingleton(typeof(ClientOptions<>));

            // Consumer-specific middleware options and classes
            services.AddScoped(typeof(DeserializerMiddleware<>));
            services.AddSingleton(typeof(ConsumerMiddlewareOptions<>));

            services.AddSingleton(typeof(ConsumerRetryMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerRetryMiddleware<>));

            services.AddSingleton(typeof(ConsumerCircuitBreakerMiddlewareOptions<>));
            services.AddScoped(typeof(ConsumerCircuitBreakerMiddleware<>));

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

            services.AddSingleton(typeof(ProducerBatchMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerBatchMiddleware<>));

            services.AddSingleton(typeof(ProducerForgetMiddlewareOptions<>));
            services.AddScoped(typeof(ProducerForgetMiddleware<>));

            services.AddSingleton(typeof(OutboxMiddlewareOptions<>));
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

            var batchOptions = (IBatchMiddlewareOptions)provider.GetRequiredService(typeof(ProducerBatchMiddlewareOptions<>).MakeGenericType(type));

            var client = provider.GetRequiredService<ClientConfig>();
            var producerConfig = new ProducerConfig(client)
            {
                BatchSize = batchOptions.BatchSize,
                MessageTimeoutMs = batchOptions.BatchTimeoutMilliseconds,
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
                .SetLogHandler((_,_) => { }).Build();
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
