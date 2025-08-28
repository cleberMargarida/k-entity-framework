using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;
using Xunit;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Base class for integration tests that provides TestContainers for Kafka and database
    /// </summary>
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly KafkaContainer KafkaContainer;
        protected readonly PostgreSqlContainer PostgreSqlContainer;
        protected readonly MsSqlContainer MsSqlContainer;
        protected IServiceProvider ServiceProvider { get; private set; } = null!;
        protected string KafkaBootstrapServers => KafkaContainer.GetBootstrapAddress();
        protected string PostgreSqlConnectionString => PostgreSqlContainer.GetConnectionString();
        protected string MsSqlConnectionString => MsSqlContainer.GetConnectionString();

        protected IntegrationTestBase()
        {
            // Configure Kafka container
            KafkaContainer = new KafkaBuilder()
                .WithImage("confluentinc/cp-kafka:7.4.0")
                .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
                .WithEnvironment("KAFKA_NUM_PARTITIONS", "1")
                .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "1")
                .Build();

            // Configure PostgreSQL container
            PostgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            // Configure SQL Server container
            MsSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("TestPass123!")
                .Build();
        }

        public virtual async Task InitializeAsync()
        {
            // Start all containers in parallel
            await Task.WhenAll(
                KafkaContainer.StartAsync(),
                PostgreSqlContainer.StartAsync(),
                MsSqlContainer.StartAsync()
            );

            // Set up services after containers are ready
            await SetupServicesAsync();
        }

        protected virtual async Task SetupServicesAsync()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            // Configure services
            await ConfigureServicesAsync(services);
            
            ServiceProvider = services.BuildServiceProvider();
        }

        protected abstract Task ConfigureServicesAsync(IServiceCollection services);

        public virtual async Task DisposeAsync()
        {
            (ServiceProvider as IDisposable)?.Dispose();
            
            // Stop all containers in parallel
            await Task.WhenAll(
                KafkaContainer.DisposeAsync().AsTask(),
                PostgreSqlContainer.DisposeAsync().AsTask(),
                MsSqlContainer.DisposeAsync().AsTask()
            );
        }

        /// <summary>
        /// Creates a scoped service provider for test isolation
        /// </summary>
        protected IServiceScope CreateScope() => ServiceProvider.CreateScope();

        /// <summary>
        /// Gets a service from the container
        /// </summary>
        protected T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

        /// <summary>
        /// Gets a service from a specific scope
        /// </summary>
        protected T GetService<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Test context for PostgreSQL database scenarios
    /// </summary>
    public class PostgreSqlTestDbContext : DbContext
    {
        public PostgreSqlTestDbContext(DbContextOptions<PostgreSqlTestDbContext> options) : base(options) { }

        public DbSet<TestMessage> Messages => Set<TestMessage>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entities for testing
            modelBuilder.Entity<TestMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.MessageType).IsRequired();
            });
        }
    }

    /// <summary>
    /// Test context for SQL Server database scenarios
    /// </summary>
    public class SqlServerTestDbContext : DbContext
    {
        public SqlServerTestDbContext(DbContextOptions<SqlServerTestDbContext> options) : base(options) { }

        public DbSet<TestMessage> Messages => Set<TestMessage>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entities for testing
            modelBuilder.Entity<TestMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.MessageType).IsRequired();
            });
        }
    }

    /// <summary>
    /// Test message entity for integration tests
    /// </summary>
    public class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MessageType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration options for integration tests
    /// </summary>
    public class IntegrationTestOptions
    {
        public bool EnableOutbox { get; set; } = true;
        public bool EnableInbox { get; set; } = true;
        public bool EnableImmediateProcessing { get; set; } = false;
        public string DatabaseProvider { get; set; } = "PostgreSQL"; // PostgreSQL or SqlServer
    }

    /// <summary>
    /// Test scenario configurations
    /// </summary>
    public static class TestScenarios
    {
        public static IntegrationTestOptions OutboxOnly => new()
        {
            EnableOutbox = true,
            EnableInbox = false,
            EnableImmediateProcessing = false
        };

        public static IntegrationTestOptions InboxOnly => new()
        {
            EnableOutbox = false,
            EnableInbox = true,
            EnableImmediateProcessing = false
        };

        public static IntegrationTestOptions OutboxAndInbox => new()
        {
            EnableOutbox = true,
            EnableInbox = true,
            EnableImmediateProcessing = false
        };

        public static IntegrationTestOptions ImmediateProcessing => new()
        {
            EnableOutbox = true,
            EnableInbox = true,
            EnableImmediateProcessing = true
        };

        public static IntegrationTestOptions Disabled => new()
        {
            EnableOutbox = false,
            EnableInbox = false,
            EnableImmediateProcessing = false
        };
    }
}
