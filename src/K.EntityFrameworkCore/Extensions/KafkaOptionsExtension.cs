global using IProducer = Confluent.Kafka.IProducer<string, byte[]>;
global using IConsumer = Confluent.Kafka.IConsumer<string, byte[]>;

using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Inbox;


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
            services.AddSingleton(typeof(InboxMiddlewareSettings<>));
            services.AddScoped(typeof(InboxMiddleware<>));

            services.AddSingleton(typeof(SubscriptionHandler<>));
            services.AddSingleton(typeof(ConsumerMiddlewareSettings<>));

            // Register channel options for configuration
            services.AddScoped(typeof(DeserializerMiddleware<>));

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
            services.AddSingleton(_ => (ConsumerConfig)client.Consumer);
            services.AddSingleton(_ => (IConsumerProcessingConfig)client.Consumer);

            // https://github.com/confluentinc/confluent-kafka-dotnet/issues/197
            // One consumer per process
            services.AddSingleton(ConsumerFactory);

            // Register the central Kafka consumer poll service as a singleton that starts lazily
            services.AddSingleton<KafkaConsumerPollService>();

            // https://github.com/confluentinc/confluent-kafka-dotnet/issues/1346
            // One producer per process
            services.AddSingleton(ProducerFactory);

            //TODO: move to source generator is okay reflection here?
            foreach (var type in contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop =>
                    prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(Topic<>)))
                .Select(prop => prop.PropertyType.GenericTypeArguments[0]))
            {
                Type serviceType = typeof(ConsumerMiddleware<>).MakeGenericType(type!);

                services.AddKeyedScoped(serviceType, type, serviceType);
                services.AddKeyedSingleton(type, (_,_) => new ConsumerConfig((ConsumerConfig)client.Consumer));
                services.AddKeyedSingleton<IConsumerConfig>(type, (_,_) => new ConsumerConfigInternal(client.ClientConfig));
                services.AddKeyedScoped(type, (_, type) => (IConsumeResultChannel)_.GetRequiredKeyedService(serviceType, type));
                services.AddKeyedScoped<IConsumer>(type, KeyedConsumerFactory);
                services.AddKeyedScoped<KafkaConsumerPollService>(type, (provider, key) => new KafkaConsumerPollService(provider, provider.GetRequiredKeyedService<IConsumer>(key)));
            }
        }

        private IProducer ProducerFactory(IServiceProvider provider)
        {
            return new ProducerBuilder<string, byte[]>(provider.GetRequiredService<ProducerConfig>()).SetLogHandler((_, _) => { }).Build();//TODO handle kafka logs
        }

        private IConsumer<string, byte[]> KeyedConsumerFactory(IServiceProvider provider, object? key)
        {
            return new ConsumerBuilder<string, byte[]>(provider.GetRequiredKeyedService<ConsumerConfig>(key)).Build();
        }

        private IConsumer<string, byte[]> ConsumerFactory(IServiceProvider provider)
        {
            return new ConsumerBuilder<string, byte[]>(provider.GetRequiredService<ConsumerConfig>()).Build();
        }

        public void Validate(IDbContextOptions options)
        {
            CachedOptions = options;
        }
    }
}
