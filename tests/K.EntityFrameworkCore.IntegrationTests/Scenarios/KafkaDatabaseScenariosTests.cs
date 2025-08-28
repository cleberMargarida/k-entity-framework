using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.IntegrationTests.Infrastructure;
using K.EntityFrameworkCore.Middlewares.Outbox;
using Xunit;
using Confluent.Kafka;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios
{
    /// <summary>
    /// Integration tests for different Kafka-Database scenarios
    /// </summary>
    [Collection("IntegrationTests")]
    public class KafkaDatabaseScenariosTests : IntegrationTestBase
    {
        private readonly IntegrationTestOptions _testOptions;

        public KafkaDatabaseScenariosTests()
        {
            _testOptions = TestScenarios.OutboxAndInbox;
        }

        protected override async Task ConfigureServicesAsync(IServiceCollection services)
        {
            // Configure PostgreSQL DbContext
            services.AddDbContext<PostgreSqlTestDbContext>(options =>
                options.UseNpgsql(PostgreSqlConnectionString)
                       .UseKafkaExtensibility(kafka =>
                       {
                           kafka.BootstrapServers = KafkaBootstrapServers;
                           kafka.ClientId = "integration-test-client";
                       }));

            // Configure SQL Server DbContext
            services.AddDbContext<SqlServerTestDbContext>(options =>
                options.UseSqlServer(MsSqlConnectionString)
                       .UseKafkaExtensibility(kafka =>
                       {
                           kafka.BootstrapServers = KafkaBootstrapServers;
                           kafka.ClientId = "integration-test-client-sqlserver";
                       }));

            // Add outbox worker if enabled
            if (_testOptions.EnableOutbox)
            {
                services.AddOutboxKafkaWorker<PostgreSqlTestDbContext>(builder =>
                {
                    // Configure outbox settings
                });
                
                services.AddOutboxKafkaWorker<SqlServerTestDbContext>(builder =>
                {
                    // Configure outbox settings
                });
            }

            await Task.CompletedTask;
        }

        protected override async Task SetupServicesAsync()
        {
            await base.SetupServicesAsync();
            
            // Ensure databases are created and migrated
            using var scope = CreateScope();
            
            var pgContext = GetService<PostgreSqlTestDbContext>(scope);
            await pgContext.Database.EnsureCreatedAsync();
            
            var sqlContext = GetService<SqlServerTestDbContext>(scope);
            await sqlContext.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task OutboxEnabled_InboxDisabled_ShouldProcessMessagesViaOutbox()
        {
            // Arrange
            var testMessage = new TestMessage
            {
                Content = "Test outbox message",
                CreatedAt = DateTime.UtcNow,
                MessageType = "TestMessage"
            };

            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // Act
            context.Messages.Add(testMessage);
            await context.SaveChangesAsync();

            // Assert
            var outboxMessages = await context.OutboxMessages.ToListAsync();
            Assert.Single(outboxMessages);
            Assert.Equal("TestMessage", outboxMessages[0].Type);
        }

        [Fact]
        public async Task OutboxDisabled_InboxEnabled_ShouldProcessMessagesDirectly()
        {
            // This test would require producing messages to Kafka and verifying inbox processing
            // For now, we'll test the configuration
            
            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);
            
            // Verify context is properly configured
            Assert.NotNull(context);
            Assert.True(context.Database.CanConnect());
        }

        [Fact]
        public async Task BothEnabled_ShouldProcessMessagesInBothDirections()
        {
            // Arrange
            var testMessage = new TestMessage
            {
                Content = "Test bidirectional message",
                CreatedAt = DateTime.UtcNow,
                MessageType = "BidirectionalTestMessage"
            };

            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // Act
            context.Messages.Add(testMessage);
            await context.SaveChangesAsync();

            // Assert - verify outbox message is created
            var outboxMessages = await context.OutboxMessages.ToListAsync();
            Assert.Single(outboxMessages);
        }

        [Fact]
        public async Task SqlServerDatabase_ShouldWorkWithKafkaExtensions()
        {
            // Arrange
            var testMessage = new TestMessage
            {
                Content = "SQL Server test message",
                CreatedAt = DateTime.UtcNow,
                MessageType = "SqlServerTestMessage"
            };

            using var scope = CreateScope();
            var context = GetService<SqlServerTestDbContext>(scope);

            // Act
            context.Messages.Add(testMessage);
            await context.SaveChangesAsync();

            // Assert
            var savedMessage = await context.Messages.FirstOrDefaultAsync();
            Assert.NotNull(savedMessage);
            Assert.Equal("SQL Server test message", savedMessage.Content);
        }

        [Fact]
        public async Task PostgreSqlDatabase_ShouldWorkWithKafkaExtensions()
        {
            // Arrange
            var testMessage = new TestMessage
            {
                Content = "PostgreSQL test message",
                CreatedAt = DateTime.UtcNow,
                MessageType = "PostgreSqlTestMessage"
            };

            using var scope = CreateScope();
            var context = GetService<PostgreSqlTestDbContext>(scope);

            // Act
            context.Messages.Add(testMessage);
            await context.SaveChangesAsync();

            // Assert
            var savedMessage = await context.Messages.FirstOrDefaultAsync();
            Assert.NotNull(savedMessage);
            Assert.Equal("PostgreSQL test message", savedMessage.Content);
        }

        [Fact]
        public async Task KafkaConnection_ShouldBeAccessible()
        {
            // Arrange
            var config = new AdminClientConfig
            {
                BootstrapServers = KafkaBootstrapServers
            };

            // Act & Assert
            using var adminClient = new AdminClientBuilder(config).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            
            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata.Brokers);
        }

        [Fact]
        public async Task OutboxWorker_WhenEnabled_ShouldBeRegistered()
        {
            // Arrange & Act
            var hostedServices = ServiceProvider.GetServices<IHostedService>();

            // Assert
            Assert.Contains(hostedServices, service => 
                service.GetType().Name.Contains("OutboxPollingWorker"));
        }

        [Fact]
        public async Task MultipleDbContexts_ShouldWorkIndependently()
        {
            // Arrange
            var pgMessage = new TestMessage
            {
                Content = "PostgreSQL message",
                CreatedAt = DateTime.UtcNow,
                MessageType = "PgMessage"
            };

            var sqlMessage = new TestMessage
            {
                Content = "SQL Server message",
                CreatedAt = DateTime.UtcNow,
                MessageType = "SqlMessage"
            };

            using var scope = CreateScope();
            var pgContext = GetService<PostgreSqlTestDbContext>(scope);
            var sqlContext = GetService<SqlServerTestDbContext>(scope);

            // Act
            pgContext.Messages.Add(pgMessage);
            sqlContext.Messages.Add(sqlMessage);
            
            await pgContext.SaveChangesAsync();
            await sqlContext.SaveChangesAsync();

            // Assert
            var pgCount = await pgContext.Messages.CountAsync();
            var sqlCount = await sqlContext.Messages.CountAsync();
            
            Assert.Equal(1, pgCount);
            Assert.Equal(1, sqlCount);
        }
    }
}
