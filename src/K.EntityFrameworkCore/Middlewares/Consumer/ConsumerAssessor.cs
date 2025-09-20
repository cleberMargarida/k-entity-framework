using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerAssessor<T>
    where T : class
{
    public bool IsExclusive { get; }

    public IConsumer Consumer { get; }


    [ActivatorUtilitiesConstructor]
    public ConsumerAssessor(ConsumerPollRegistry consumerPollRegistry, IServiceProvider serviceProvider)
    {
        IModel model = serviceProvider.GetRequiredService<IModel>();

        IsExclusive = model.HasExclusiveConnection<T>();

        if (IsExclusive)
        {
            var parentConsumerConfig = serviceProvider.GetRequiredService<IConsumerConfig>();
            var consumerConfig = new ConsumerConfigInternal((ClientConfig)parentConsumerConfig);

            model.GetExclusiveConnectionConfig<T>()?.Invoke(consumerConfig);

            Consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();

            serviceProvider.GetRequiredService<Channel<T>>().Settings = consumerConfig;

            consumerPollRegistry.Register<T>(Consumer);
        }
        else
        {
            Consumer = serviceProvider.GetRequiredService<IConsumer>();
        }
    }
}
