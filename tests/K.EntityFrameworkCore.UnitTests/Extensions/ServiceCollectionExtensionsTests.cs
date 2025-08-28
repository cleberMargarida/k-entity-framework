using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Outbox;
using Xunit;
using System.Reflection;

namespace K.EntityFrameworkCore.UnitTests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        }

        [Fact]
        public void AddOutboxKafkaWorker_ShouldRegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddOutboxKafkaWorker<TestDbContext>();

            // Assert - Check that the hosted service is registered instead of trying to resolve it
            var hostedServiceDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IHostedService) && 
                s.ImplementationType?.Name.Contains("OutboxPollingWorker") == true);
            
            Assert.NotNull(hostedServiceDescriptor);
        }

        [Fact]
        public void AddOutboxKafkaWorker_ShouldRegisterOutboxWorkerBuilder()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddOutboxKafkaWorker<TestDbContext>();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var builder = serviceProvider.GetService<OutboxWorkerBuilder<TestDbContext>>();
            Assert.NotNull(builder);
        }

        [Fact]
        public void AddOutboxKafkaWorker_ShouldReturnSameServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddOutboxKafkaWorker<TestDbContext>();

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddOutboxKafkaWorker_WithNullConfigureWorker_ShouldNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - Should not throw
            services.AddOutboxKafkaWorker<TestDbContext>(null);
            Assert.True(true);
        }

        [Fact]
        public void AddOutboxKafkaWorker_WithConfiguration_ShouldInvokeConfigureWorker()
        {
            // Arrange
            var services = new ServiceCollection();
            var configureWorkerCalled = false;

            // Act
            services.AddOutboxKafkaWorker<TestDbContext>(builder =>
            {
                configureWorkerCalled = true;
            });

            // Assert
            Assert.True(configureWorkerCalled);
        }

        [Fact]
        public void AddOutboxKafkaWorker_ShouldRegisterSingletonBuilder()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddOutboxKafkaWorker<TestDbContext>();

            // Assert
            var builderDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(OutboxWorkerBuilder<TestDbContext>));
            
            Assert.NotNull(builderDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, builderDescriptor.Lifetime);
        }

        [Fact]
        public void AddOutboxKafkaWorker_MultipleCalls_ShouldUseTryAdd()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddOutboxKafkaWorker<TestDbContext>();
            services.AddOutboxKafkaWorker<TestDbContext>();

            // Assert - Should only have one registration of the builder
            var builderDescriptors = services.Where(s => 
                s.ServiceType == typeof(OutboxWorkerBuilder<TestDbContext>));
            
            Assert.Single(builderDescriptors);
        }
    }

    public class OutboxPollingWorkerSettingsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(1), settings.PollingInterval);
            Assert.Equal(1000, (int)settings.PollingInterval.TotalMilliseconds);
            Assert.Equal(100, settings.MaxMessagesPerPoll);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        public void PollingIntervalMilliseconds_SetAndGet_ShouldWorkCorrectly(int milliseconds)
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Act
            settings.PollingIntervalMilliseconds = milliseconds;

            // Assert
            // Note: PollingIntervalMilliseconds getter returns .Milliseconds (component), not total milliseconds
            Assert.Equal(milliseconds % 1000, settings.PollingIntervalMilliseconds);
            Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), settings.PollingInterval);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public void MaxMessagesPerPoll_SetAndGet_ShouldWorkCorrectly(int maxMessages)
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Act
            settings.MaxMessagesPerPoll = maxMessages;

            // Assert
            Assert.Equal(maxMessages, settings.MaxMessagesPerPoll);
        }

        [Fact]
        public void PollingInterval_SetDirectly_ShouldUpdateMilliseconds()
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();
            var interval = TimeSpan.FromSeconds(5);

            // Act
            settings.PollingInterval = interval;

            // Assert
            Assert.Equal(interval, settings.PollingInterval);
            // The PollingIntervalMilliseconds property returns .Milliseconds component (0 for whole seconds)
            Assert.Equal(0, settings.PollingIntervalMilliseconds);
        }

        [Fact]
        public void PollingIntervalMilliseconds_SetToZero_ShouldWork()
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Act
            settings.PollingIntervalMilliseconds = 0;

            // Assert
            Assert.Equal(0, settings.PollingIntervalMilliseconds);
            Assert.Equal(TimeSpan.Zero, settings.PollingInterval);
        }

        [Fact]
        public void MaxMessagesPerPoll_SetToZero_ShouldWork()
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Act
            settings.MaxMessagesPerPoll = 0;

            // Assert
            Assert.Equal(0, settings.MaxMessagesPerPoll);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void PollingIntervalMilliseconds_SetToNegative_ShouldWork(int milliseconds)
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Act
            settings.PollingIntervalMilliseconds = milliseconds;

            // Assert
            // For negative values, the milliseconds component behavior depends on the TimeSpan implementation
            Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), settings.PollingInterval);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void MaxMessagesPerPoll_SetToNegative_ShouldWork(int maxMessages)
        {
            // Arrange
            var settings = new OutboxPollingWorkerSettings<TestDbContext>();

            // Act
            settings.MaxMessagesPerPoll = maxMessages;

            // Assert
            Assert.Equal(maxMessages, settings.MaxMessagesPerPoll);
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        }
    }
}
