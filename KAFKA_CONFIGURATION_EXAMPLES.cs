using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;

namespace K.EntityFrameworkCore.Examples
{
    /// <summary>
    /// Example demonstrating how to use the enhanced Kafka configuration interfaces.
    /// </summary>
    public class KafkaConfigurationExample
    {
        /// <summary>
        /// Example showing how to configure Kafka using the new interface properties.
        /// </summary>
        public void ConfigureKafkaWithNewProperties()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseKafkaExtensibility(client =>
            {
                // Configure client-level properties
                client.BootstrapServers = "localhost:9092";
                client.ClientId = "my-client";
                client.SecurityProtocol = SecurityProtocol.SaslSsl;
                client.SaslMechanism = SaslMechanism.Plain;
                client.SaslUsername = "my-username";
                client.SaslPassword = "my-password";
                client.EnableSslCertificateVerification = true;
                client.SslCaLocation = "/path/to/ca.pem";
                client.StatisticsIntervalMs = 5000;
                client.LogConnectionClose = true;
                client.MaxInFlight = 5;
                client.MessageMaxBytes = 1000000;
                client.SocketTimeoutMs = 60000;
                client.TopicMetadataRefreshIntervalMs = 300000;

                // Configure producer-specific properties
                client.Producer.Acks = Acks.All;
                client.Producer.EnableIdempotence = true;
                client.Producer.BatchSize = 16384;
                client.Producer.LingerMs = 5.0;
                client.Producer.CompressionType = CompressionType.Snappy;
                client.Producer.MessageTimeoutMs = 300000;
                client.Producer.RequestTimeoutMs = 30000;
                client.Producer.EnableDeliveryReports = true;
                client.Producer.MessageSendMaxRetries = 2147483647;
                client.Producer.QueueBufferingMaxMessages = 100000;
                client.Producer.QueueBufferingMaxKbytes = 1048576;
                client.Producer.TransactionalId = "my-transactional-id";

                // Configure consumer-specific properties
                client.Consumer.GroupId = "my-consumer-group";
                client.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
                client.Consumer.EnableAutoCommit = true;
                client.Consumer.AutoCommitIntervalMs = 5000;
                client.Consumer.SessionTimeoutMs = 10000;
                client.Consumer.HeartbeatIntervalMs = 3000;
                client.Consumer.MaxPollIntervalMs = 300000;
                client.Consumer.FetchMinBytes = 1;
                client.Consumer.FetchMaxBytes = 52428800;
                client.Consumer.MaxPartitionFetchBytes = 1048576;
                client.Consumer.EnablePartitionEof = false;
                client.Consumer.CheckCrcs = true;
                client.Consumer.IsolationLevel = IsolationLevel.ReadCommitted;
                client.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky;
            });
        }

        /// <summary>
        /// Example showing how to access the underlying Confluent.Kafka ClientConfig.
        /// </summary>
        public void AccessUnderlyingClientConfig()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseKafkaExtensibility(client =>
            {
                // Access the underlying ClientConfig for advanced scenarios
                var underlyingConfig = client.ClientConfig;
                
                // You can still set properties directly on the underlying config if needed
                underlyingConfig.Set("custom.property", "custom.value");
                
                // Or use the strongly-typed interface properties
                client.BootstrapServers = "localhost:9092";
                client.Producer.Acks = Acks.All;
                client.Consumer.GroupId = "my-group";
            });
        }

        /// <summary>
        /// Example showing SSL/TLS configuration.
        /// </summary>
        public void ConfigureSslTls()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseKafkaExtensibility(client =>
            {
                client.SecurityProtocol = SecurityProtocol.Ssl;
                client.SslCaLocation = "/path/to/ca-cert.pem";
                client.SslCertificateLocation = "/path/to/client-cert.pem";
                client.SslKeyLocation = "/path/to/client-key.pem";
                client.SslKeyPassword = "key-password";
                client.EnableSslCertificateVerification = true;
                client.SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https;
            });
        }

        /// <summary>
        /// Example showing SASL authentication configuration.
        /// </summary>
        public void ConfigureSaslAuthentication()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseKafkaExtensibility(client =>
            {
                client.SecurityProtocol = SecurityProtocol.SaslSsl;
                client.SaslMechanism = SaslMechanism.ScramSha256;
                client.SaslUsername = "my-username";
                client.SaslPassword = "my-password";
                
                // Or for OAuth Bearer
                client.SaslMechanism = SaslMechanism.OAuthBearer;
                client.SaslOauthbearerTokenEndpointUrl = "https://auth.example.com/token";
                client.SaslOauthbearerClientId = "my-client-id";
                client.SaslOauthbearerClientSecret = "my-client-secret";
                client.SaslOauthbearerScope = "kafka";
            });
        }

        /// <summary>
        /// Example showing performance tuning configuration.
        /// </summary>
        public void ConfigurePerformanceTuning()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseKafkaExtensibility(client =>
            {
                // Network settings
                client.SocketSendBufferBytes = 131072;
                client.SocketReceiveBufferBytes = 131072;
                client.SocketTimeoutMs = 60000;
                client.MaxInFlight = 5;
                
                // Producer performance settings
                client.Producer.BatchSize = 65536;
                client.Producer.LingerMs = 10.0;
                client.Producer.CompressionType = CompressionType.Lz4;
                client.Producer.QueueBufferingMaxKbytes = 2097152;
                
                // Consumer performance settings
                client.Consumer.FetchMinBytes = 1024;
                client.Consumer.FetchMaxBytes = 104857600;
                client.Consumer.MaxPartitionFetchBytes = 2097152;
                client.Consumer.QueuedMaxMessagesKbytes = 1048576;
            });
        }
    }
}
