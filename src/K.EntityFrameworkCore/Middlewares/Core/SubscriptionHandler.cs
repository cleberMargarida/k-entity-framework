using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace K.EntityFrameworkCore.Middlewares.Core
{
    internal class SubscriptionHandler<T>
        where T : class
    {
        private readonly IConsumer consumer;
        private readonly ClientSettings<T> clientSettings;

        public SubscriptionHandler(ClientSettings<T> clientSettings, IServiceProvider serviceProvider)
        {
            this.clientSettings = clientSettings;

            if (serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>().ExclusiveConnection == true)
            {
                serviceProvider.GetRequiredKeyedService<KafkaConsumerPollService>(typeof(T)).EnsureStarted();
                consumer = serviceProvider.GetRequiredKeyedService<IConsumer>(typeof(T));
                return;
            }

            serviceProvider.GetRequiredService<KafkaConsumerPollService>().EnsureStarted();
            consumer = serviceProvider.GetRequiredService<IConsumer>();
        }

        [field: AllowNull]
        private string Topic => field ??= clientSettings.TopicName;

        public void Subscribe()
        {
            var assignments = consumer.Assignment.Select(TopicName).ToHashSet();

            assignments.Add(Topic);

            consumer.Subscribe(assignments);
        }

        public void Unsubscribe()
        {
            var assignments = consumer.Assignment.Select(TopicName).ToHashSet();
            assignments.Remove(Topic);

            if (assignments.Count > 0)
                consumer.Subscribe(assignments);
            else
                consumer.Unsubscribe();
        }

        private static string TopicName(TopicPartition topicPartition) => topicPartition.Topic;
    }
}