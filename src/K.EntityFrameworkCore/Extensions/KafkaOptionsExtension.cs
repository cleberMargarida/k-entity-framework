global using IProducer = Confluent.Kafka.IProducer<string, byte[]>;

using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using K.EntityFrameworkCore.Middlewares.Core;


namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaOptionsExtension : IDbContextOptionsExtension
    {
        // TODO dictionary of context types to options
        internal static IDbContextOptions? CachedOptions;

        private readonly KafkaClientBuilder client;
        private readonly Type contextType;

        public KafkaOptionsExtension(KafkaClientBuilder client, Type contextType)
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

            services.AddSingleton(typeof(ConsumerForgetMiddlewareSettings<>));
            services.AddScoped(typeof(ConsumerForgetMiddleware<>));

            // Producer-specific middleware options and classes
            services.AddSingleton(typeof(ProducerMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerMiddleware<>));

            services.AddScoped(typeof(SerializerMiddleware<>));

            services.AddSingleton(typeof(ProducerForgetMiddlewareSettings<>));
            services.AddScoped(typeof(ProducerForgetMiddleware<>));

            services.AddSingleton(typeof(OutboxMiddlewareSettings<>));
            services.AddScoped(typeof(OutboxMiddleware<>));
            services.AddSingleton(typeof(OutboxProducerMiddleware<>));

            services.AddSingleton(_ => client.ClientConfig);
            services.AddSingleton(_ => (ProducerConfig)client.Producer);
            services.AddSingleton(_ => (ConsumerConfig)client.ClientConfig);

            // https://github.com/confluentinc/confluent-kafka-dotnet/issues/197
            // One consumer per process
            services.AddSingleton(ConsumerFactory);

            // https://github.com/confluentinc/confluent-kafka-dotnet/issues/1346
            // One producer per process
            services.AddSingleton(ProducerFactory);

            //TODO: move to source generator is okay reflection here?
            foreach (var item in contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => 
                    prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(Topic<>)))
                .Select(prop => prop.PropertyType.GenericTypeArguments[0]))
            {
                //services.AddKeyedSingleton<IProducer>(item, ProducerFactory);
            }
        }

        private IProducer ProducerFactory(IServiceProvider provider)
        {
            return new ProducerBuilder<string, byte[]>(provider.GetRequiredService<ProducerConfig>()).SetLogHandler((_, _) => { }).Build();//TODO handle kafka logs
        }

        private IConsumer<Ignore, byte[]> ConsumerFactory(IServiceProvider provider)
        {
            return new ConsumerBuilder<Ignore, byte[]>(provider.GetRequiredService<ConsumerConfig>()).Build();
        }

        public void Validate(IDbContextOptions options)
        {
            CachedOptions = options;
        }
    }
}
