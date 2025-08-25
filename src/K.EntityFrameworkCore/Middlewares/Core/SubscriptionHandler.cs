using Confluent.Kafka;
using System.Diagnostics.CodeAnalysis;

namespace K.EntityFrameworkCore.Middlewares.Core
{
    internal class SubscriptionHandler<T>(IConsumer consumer, ClientSettings<T> clientSettings) 
        where T : class
    {
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