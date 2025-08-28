using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using K.EntityFrameworkCore.Extensions;
using Confluent.Kafka;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Extensions
{
    public class DbContextOptionsBuilderExtensionsTests
    {
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        }

        [Fact]
        public void UseKafkaExtensibility_ShouldConfigureDbContext()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            // Act
            var result = optionsBuilder.UseKafkaExtensibility(client =>
            {
                client.BootstrapServers = "localhost:9092";
            });

            // Assert
            Assert.Same(optionsBuilder, result);
            Assert.NotNull(optionsBuilder.Options);
        }

        [Fact]
        public void UseKafkaExtensibility_ShouldSetupWithValidConfiguration()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            const string bootstrapServers = "localhost:9092,localhost:9093";
            const string clientId = "test-client";

            // Act
            optionsBuilder.UseKafkaExtensibility(client =>
            {
                client.BootstrapServers = bootstrapServers;
                client.ClientId = clientId;
                client.AllowAutoCreateTopics = true;
                client.Acks = Acks.All;
            });

            // Assert - Test passes if no exception is thrown
            Assert.True(true);
        }

        [Fact]
        public void UseKafkaExtensibility_ShouldAcceptNullValues()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            // Act & Assert - Should not throw
            optionsBuilder.UseKafkaExtensibility(client =>
            {
                client.BootstrapServers = null;
                client.ClientId = null;
                client.AllowAutoCreateTopics = null;
                client.Acks = null;
            });

            Assert.True(true);
        }
    }

    public class KafkaClientBuilderTests
    {
        [Fact]
        public void Constructor_WithClientConfig_ShouldSetProperties()
        {
            // Arrange
            var clientConfig = new ClientConfig
            {
                BootstrapServers = "localhost:9092",
                ClientId = "test-client"
            };

            // Act
            var builder = new KafkaClientBuilder(clientConfig);

            // Assert
            Assert.Equal("localhost:9092", builder.BootstrapServers);
            Assert.Equal("test-client", builder.ClientId);
        }

        [Theory]
        [InlineData("localhost:9092")]
        [InlineData("broker1:9092,broker2:9092")]
        [InlineData("")]
        [InlineData(null)]
        public void BootstrapServers_SetAndGet_ShouldWorkCorrectly(string? servers)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.BootstrapServers = servers;

            // Assert
            Assert.Equal(servers, builder.BootstrapServers);
        }

        [Theory]
        [InlineData("test-client")]
        [InlineData("")]
        [InlineData(null)]
        public void ClientId_SetAndGet_ShouldWorkCorrectly(string? clientId)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.ClientId = clientId;

            // Assert
            Assert.Equal(clientId, builder.ClientId);
        }

        [Theory]
        [InlineData(Acks.None)]
        [InlineData(Acks.Leader)]
        [InlineData(Acks.All)]
        [InlineData(null)]
        public void Acks_SetAndGet_ShouldWorkCorrectly(Acks? acks)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.Acks = acks;

            // Assert
            Assert.Equal(acks, builder.Acks);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void AllowAutoCreateTopics_SetAndGet_ShouldWorkCorrectly(bool? allowAutoCreate)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.AllowAutoCreateTopics = allowAutoCreate;

            // Assert
            Assert.Equal(allowAutoCreate, builder.AllowAutoCreateTopics);
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(0)]
        [InlineData(null)]
        public void ApiVersionFallbackMs_SetAndGet_ShouldWorkCorrectly(int? fallbackMs)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.ApiVersionFallbackMs = fallbackMs;

            // Assert
            Assert.Equal(fallbackMs, builder.ApiVersionFallbackMs);
        }

        [Fact]
        public void Producer_ShouldReturnProducerConfig()
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            var producer = builder.Producer;

            // Assert
            Assert.NotNull(producer);
            Assert.IsAssignableFrom<IProducerConfig>(producer);
        }

        [Fact]
        public void Consumer_ShouldReturnConsumerConfig()
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            var consumer = builder.Consumer;

            // Assert
            Assert.NotNull(consumer);
            Assert.IsAssignableFrom<IConsumerConfig>(consumer);
        }

        [Fact]
        public void Producer_ShouldBeLazy_ReturnsSameInstance()
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            var producer1 = builder.Producer;
            var producer2 = builder.Producer;

            // Assert
            Assert.Same(producer1, producer2);
        }

        [Fact]
        public void Consumer_ShouldBeLazy_ReturnsSameInstance()
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            var consumer1 = builder.Consumer;
            var consumer2 = builder.Consumer;

            // Assert
            Assert.Same(consumer1, consumer2);
        }

        [Theory]
        [InlineData(ClientDnsLookup.UseAllDnsIps)]
        [InlineData(ClientDnsLookup.ResolveCanonicalBootstrapServersOnly)]
        [InlineData(null)]
        public void ClientDnsLookup_SetAndGet_ShouldWorkCorrectly(ClientDnsLookup? dnsLookup)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.ClientDnsLookup = dnsLookup;

            // Assert
            Assert.Equal(dnsLookup, builder.ClientDnsLookup);
        }

        [Theory]
        [InlineData(BrokerAddressFamily.V4)]
        [InlineData(BrokerAddressFamily.V6)]
        [InlineData(BrokerAddressFamily.Any)]
        [InlineData(null)]
        public void BrokerAddressFamily_SetAndGet_ShouldWorkCorrectly(BrokerAddressFamily? addressFamily)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.BrokerAddressFamily = addressFamily;

            // Assert
            Assert.Equal(addressFamily, builder.BrokerAddressFamily);
        }

        [Theory]
        [InlineData("debug")]
        [InlineData("broker,protocol")]
        [InlineData("")]
        [InlineData(null)]
        public void Debug_SetAndGet_ShouldWorkCorrectly(string? debug)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.Debug = debug;

            // Assert
            Assert.Equal(debug, builder.Debug);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void ApiVersionRequest_SetAndGet_ShouldWorkCorrectly(bool? apiVersionRequest)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.ApiVersionRequest = apiVersionRequest;

            // Assert
            Assert.Equal(apiVersionRequest, builder.ApiVersionRequest);
        }

        [Theory]
        [InlineData(5000)]
        [InlineData(10000)]
        [InlineData(0)]
        [InlineData(null)]
        public void ApiVersionRequestTimeoutMs_SetAndGet_ShouldWorkCorrectly(int? timeout)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.ApiVersionRequestTimeoutMs = timeout;

            // Assert
            Assert.Equal(timeout, builder.ApiVersionRequestTimeoutMs);
        }

        [Theory]
        [InlineData(60000)]
        [InlineData(30000)]
        [InlineData(0)]
        [InlineData(null)]
        public void BrokerAddressTtl_SetAndGet_ShouldWorkCorrectly(int? ttl)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.BrokerAddressTtl = ttl;

            // Assert
            Assert.Equal(ttl, builder.BrokerAddressTtl);
        }

        [Theory]
        [InlineData("1.0.0")]
        [InlineData("2.8.0")]
        [InlineData("")]
        [InlineData(null)]
        public void BrokerVersionFallback_SetAndGet_ShouldWorkCorrectly(string? version)
        {
            // Arrange
            var clientConfig = new ClientConfig();
            var builder = new KafkaClientBuilder(clientConfig);

            // Act
            builder.BrokerVersionFallback = version;

            // Assert
            Assert.Equal(version, builder.BrokerVersionFallback);
        }
    }
}
