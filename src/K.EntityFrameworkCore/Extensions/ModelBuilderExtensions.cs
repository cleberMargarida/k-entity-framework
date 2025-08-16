using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ModelBuilder"/> to configure domain events.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder Topic<T>(this ModelBuilder modelBuilder, Action<TopicTypeBuilder<T>> topic) where T : class
        {
            topic(new TopicTypeBuilder<T>(modelBuilder));
            return modelBuilder;
        }
    }

    public class TopicTypeBuilder<T>(ModelBuilder modelBuilder)
        where T : class
    {
        public TopicTypeBuilder<T> HasConsumer(Action<ConsumerBuilder<T>> consumer)
        {
            return this;
        }

        public TopicTypeBuilder<T> HasName(string name)
        {
            return this;
        }

        public TopicTypeBuilder<T> HasProducer(Action<ProducerBuilder<T>> producer)
        {
            return this;
        }

        public TopicTypeBuilder<T> HasSetting(Action<TopicSpecification> settings)
        {
            return this;
        }

        public TopicTypeBuilder<T> UseJsonSerializer(Action<JsonSerializerOptions> jsonSerializerOptions)
        {
            return this;
        }
    }

    public class ProducerBuilder<T>
        where T : class
    {
        public ProducerBuilder<T> HasKey<TProp>(Expression<Func<T, TProp>> keyPropertyAccessor)
        {
            return this;
        }
    }

    public class ConsumerBuilder<T>
        where T : class
    {
        public ConsumerBuilder<T> GroupId(string value)
        {
            return this;
        }
    }
}
