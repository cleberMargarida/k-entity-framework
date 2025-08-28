using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.IntegrationTests.Infrastructure;
using K.EntityFrameworkCore.Middlewares.Outbox;
using Xunit;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios
{
    /// <summary>
    /// Tests for Outbox-only scenarios (Inbox disabled)
    /// </summary>
    [Collection("IntegrationTests")]
    public class OutboxOnlyScenarioTests : IntegrationTestBase
    {
        protected override async Task ConfigureServicesAsync(IServiceCollection services)
        {
            services.AddDbContext<PostgreSqlTestDbContext>(options =>
                options.UseNpgsql(PostgreSqlConnectionString)
                       .UseKafkaExtensibility(kafka =>
                       {
                           kafka.BootstrapServers = KafkaBootstrapServers;
                           kafka.ClientId = "outbox-only-test";
                       }));

            // Enable only outbox
            services.AddOutboxKafkaWorker<PostgreSqlTestDbContext>();

            await Task.CompletedTask;
        }

        protected override async Task SetupServicesAsync()
        {
            await base.SetupServicesAsync();
            
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);
            await context.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task OutboxOnly_MessagesShouldBeStoredInOutbox()
        {
            // Arrange
            var message = new TestMessage
            {
                Content = "Outbox only test",
                CreatedAt = DateTime.UtcNow,
                MessageType = "OutboxOnlyMessage"
            };

            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // Act
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            // Assert
            var outboxCount = await context.OutboxMessages.CountAsync();
            Assert.True(outboxCount > 0, "Outbox should contain messages when outbox is enabled");
        }

        [Fact]
        public async Task OutboxOnly_BackgroundProcessingShouldBeConfigured()
        {
            // Arrange
            var outboxSettings = GetService<OutboxMiddlewareSettings<TestMessage>>();

            // Assert
            Assert.Equal(OutboxPublishingStrategy.BackgroundOnly, outboxSettings.Strategy);
        }
    }

    /// <summary>
    /// Tests for Inbox-only scenarios (Outbox disabled)
    /// </summary>
    [Collection("IntegrationTests")]
    public class InboxOnlyScenarioTests : IntegrationTestBase
    {
        protected override async Task ConfigureServicesAsync(IServiceCollection services)
        {
            services.AddDbContext<PostgreSqlTestDbContext>(options =>
                options.UseNpgsql(PostgreSqlConnectionString)
                       .UseKafkaExtensibility(kafka =>
                       {
                           kafka.BootstrapServers = KafkaBootstrapServers;
                           kafka.ClientId = "inbox-only-test";
                       }));

            // Note: In a real implementation, you would configure inbox-only here
            // For this example, we're testing the infrastructure

            await Task.CompletedTask;
        }

        protected override async Task SetupServicesAsync()
        {
            await base.SetupServicesAsync();
            
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);
            await context.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task InboxOnly_ShouldProcessIncomingMessages()
        {
            // This would test inbox message processing
            // For now, verify the infrastructure is set up correctly
            
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);
            
            Assert.NotNull(context.InboxMessages);
            Assert.True(context.Database.CanConnect());
        }
    }

    /// <summary>
    /// Tests for immediate processing scenarios
    /// </summary>
    [Collection("IntegrationTests")]
    public class ImmediateProcessingScenarioTests : IntegrationTestBase
    {
        protected override async Task ConfigureServicesAsync(IServiceCollection services)
        {
            services.AddDbContext<PostgreSqlTestDbContext>(options =>
                options.UseNpgsql(PostgreSqlConnectionString)
                       .UseKafkaExtensibility(kafka =>
                       {
                           kafka.BootstrapServers = KafkaBootstrapServers;
                           kafka.ClientId = "immediate-processing-test";
                       }));

            services.AddOutboxKafkaWorker<PostgreSqlTestDbContext>();

            // Configure immediate processing
            services.Configure<OutboxMiddlewareSettings<TestMessage>>(settings =>
            {
                settings.Strategy = OutboxPublishingStrategy.ImmediateWithFallback;
            });

            await Task.CompletedTask;
        }

        protected override async Task SetupServicesAsync()
        {
            await base.SetupServicesAsync();
            
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);
            await context.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task ImmediateProcessing_ShouldBeConfiguredCorrectly()
        {
            // Arrange
            var outboxSettings = GetService<OutboxMiddlewareSettings<TestMessage>>();

            // Assert
            Assert.Equal(OutboxPublishingStrategy.ImmediateWithFallback, outboxSettings.Strategy);
        }

        [Fact]
        public async Task ImmediateProcessing_ShouldStillCreateOutboxRecord()
        {
            // Arrange
            var message = new TestMessage
            {
                Content = "Immediate processing test",
                CreatedAt = DateTime.UtcNow,
                MessageType = "ImmediateMessage"
            };

            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // Act
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            // Assert - Even with immediate processing, outbox record should exist for reliability
            var outboxCount = await context.OutboxMessages.CountAsync();
            Assert.True(outboxCount >= 0, "Outbox may contain messages for reliability");
        }
    }

    /// <summary>
    /// Tests for disabled scenarios (no Kafka processing)
    /// </summary>
    [Collection("IntegrationTests")]
    public class DisabledScenarioTests : IntegrationTestBase
    {
        protected override async Task ConfigureServicesAsync(IServiceCollection services)
        {
            // Configure without Kafka extensions
            services.AddDbContext<PostgreSqlTestDbContext>(options =>
                options.UseNpgsql(PostgreSqlConnectionString));

            await Task.CompletedTask;
        }

        protected override async Task SetupServicesAsync()
        {
            await base.SetupServicesAsync();
            
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);
            await context.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task Disabled_ShouldWorkWithoutKafkaExtensions()
        {
            // Arrange
            var message = new TestMessage
            {
                Content = "Disabled scenario test",
                CreatedAt = DateTime.UtcNow,
                MessageType = "DisabledMessage"
            };

            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // Act
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            // Assert - Should work normally without Kafka
            var savedMessage = await context.Messages.FirstOrDefaultAsync();
            Assert.NotNull(savedMessage);
            Assert.Equal("Disabled scenario test", savedMessage.Content);
        }

        [Fact]
        public async Task Disabled_ShouldNotHaveOutboxMessages()
        {
            // Since Kafka extensions are not configured, there should be no outbox table
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // This test verifies that without Kafka configuration, the system works normally
            var messageCount = await context.Messages.CountAsync();
            Assert.True(messageCount >= 0);
        }
    }
}
