using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Extension methods for configuring Kafka in a DbContext.
    /// </summary>
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the DbContext to use Kafka with the specified client configuration.
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder UseKafkaExtensibility(this DbContextOptionsBuilder optionsBuilder, Action<KafkaClientBuilder> client)
        {
            var clientInstance = new KafkaClientBuilder(new ClientConfig());
            client.Invoke(clientInstance);
            IDbContextOptionsBuilderInfrastructure infrastructure = optionsBuilder;
            infrastructure.AddOrUpdateExtension(new KafkaOptionsExtension(clientInstance, optionsBuilder.Options.ContextType));
            optionsBuilder.AddInterceptors(new KafkaMiddlewareInterceptor());
            optionsBuilder.ReplaceService<IDbSetInitializer, DbSetInitializerExt>();
            return optionsBuilder;
        }
    }

    public class KafkaClientBuilder(
        ClientConfig clientConfig,
        ProducerConfig producerConfig,
        ConsumerConfig consumerConfig)
    {
        internal KafkaClientBuilder(ClientConfig clientConfig) : this(
            clientConfig,
            new ProducerConfig(clientConfig),
            new ConsumerConfig(clientConfig))
        {
        }


    }
}
