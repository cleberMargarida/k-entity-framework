global using IConsumer = Confluent.Kafka.IConsumer<string, byte[]>;
global using IProducer = Confluent.Kafka.IProducer<string, byte[]>;
using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Consumer;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.HeaderFilter;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Producer;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Error = Confluent.Kafka.Error;

namespace K.EntityFrameworkCore.Extensions;

internal class KafkaOptionsExtension : IDbContextOptionsExtension
{
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
        services.AddLogging();
        services.AddSingleton(this.client);
        services.AddSingleton(this.client.ClientConfig);
        services.AddSingleton(this.client.Producer);
        services.AddSingleton(this.client.Consumer);
        services.AddSingleton<IConsumerProcessingConfig>(this.client.Consumer);

        services.AddSingleton(typeof(ConsumerPollRegistry));

        services.AddScoped(typeof(ScopedCommandRegistry));
        services.AddScoped(typeof(ClientSettings<>));
        services.AddScoped(typeof(ProducerMiddlewareInvoker<>));
        services.AddScoped(typeof(ProducerMiddlewareSettings<>));
        services.AddScoped(typeof(ProducerMiddleware<>));

        services.AddScoped(typeof(SerializationMiddlewareSettings<>));
        services.AddScoped(typeof(SerializerMiddleware<>));
        services.AddScoped(typeof(DeserializerMiddleware<>));

        services.AddScoped(typeof(OutboxMiddlewareSettings<>));
        services.AddScoped(typeof(OutboxMiddleware<>));
        services.AddScoped(typeof(OutboxProducerMiddleware<>));

        //services.AddScoped(typeof(ForgetMiddlewareSettings<>));
        //services.AddScoped(typeof(ForgetMiddleware<>));

        services.AddSingleton(typeof(Channel<>));

        services.AddScoped(typeof(ConsumerMiddlewareInvoker<>));
        services.AddScoped(typeof(SubscriberMiddleware<>));
        services.AddScoped(typeof(ConsumerMiddlewareSettings<>));
        services.AddScoped(typeof(ConsumerMiddleware<>));

        services.AddScoped(typeof(InboxMiddlewareSettings<>));
        services.AddScoped(typeof(InboxMiddleware<>));

        services.AddScoped(typeof(HeaderFilterMiddlewareSettings<>));
        services.AddScoped(typeof(HeaderFilterMiddleware<>));

        services.AddSingleton(provider =>
        {
            ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("K.EntityFrameworkCore.Producer");

            return new ProducerBuilder<string, byte[]>((ProducerConfig)this.client.Producer)
                .SetLogHandler(OnLog)
                .SetErrorHandler(OnError)
                .Build();

            void OnError(IProducer producer, Error error)
            {
                logger.LogError(
                    "Kafka producer '{ClientId}' on host '{HostName}' encountered a error: {ErrorReason} (Code: {ErrorCode}, Broker: {BrokerName})",
                    this.client.ClientId,
                    Environment.MachineName,
                    error.Reason,
                    error.Code,
                    error.IsLocalError ? "local" : "broker");
            }

            void OnLog(IProducer producer, LogMessage logMessage)
            {
                logger.Log((LogLevel)logMessage.LevelAs(LogLevelType.MicrosoftExtensionsLogging),
                    "Kafka client log [{Facility}] from group '{ClientId}' on host '{HostName}': {Message}",
                    logMessage.Facility,
                    this.client.ClientId,
                    Environment.MachineName,
                    logMessage.Message);
            }
        });

        services.AddSingleton(provider =>
        {
            ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("K.EntityFrameworkCore.Consumer");

            return new ConsumerBuilder<string, byte[]>((ConsumerConfig)this.client.Consumer)
                .Build();
        });

        services.AddScoped(typeof(ConsumerAssessor<>));

        ////TODO: move to source generator? is okay reflection here?
        //foreach (var type in this.contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        //    .Where(prop =>
        //        prop.PropertyType.IsGenericType &&
        //        prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(Topic<>)))
        //    .Select(prop => prop.PropertyType.GenericTypeArguments[0]))
        //{
        //}
    }

    public void Validate(IDbContextOptions options)
    {
    }
}
