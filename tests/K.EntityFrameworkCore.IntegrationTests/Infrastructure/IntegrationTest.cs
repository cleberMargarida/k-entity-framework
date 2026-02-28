using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that uses shared TestContainers via fixture
/// </summary>
[SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Properties has overheads for tests")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Is necessary")]
public abstract class IntegrationTest : IDisposable
{
    private readonly KafkaFixture kafka;

    private IHost host;
    protected ModelBuilder modelBuilder = new();
    protected WebApplicationBuilder builder;
    protected PostgreTestContext context;
    protected TopicTypeBuilder<DefaultMessage> defaultTopic;
    protected TopicTypeBuilder<AlternativeMessage> alternativeTopic;

    public IntegrationTest(KafkaFixture kafka, PostgreSqlFixture postgreSql)
    {
        this.kafka = kafka;

        builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        Environment.SetEnvironmentVariable("ConnectionStrings:kafka", kafka.BootstrapAddress);
        Environment.SetEnvironmentVariable("ConnectionStrings:postgres", postgreSql.Connection);

        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddDbContext<PostgreTestContext>(
            options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"))
                       .EnableServiceProviderCaching(false)
                       .UseKafkaExtensibility(builder.Configuration.GetConnectionString("kafka")));

        defaultTopic = new TopicTypeBuilder<DefaultMessage>(modelBuilder);
        alternativeTopic = new TopicTypeBuilder<AlternativeMessage>(modelBuilder);
    }

    protected async Task StartHostAsync()
    {
        host = builder.Build();

        context = host.Services.GetService<PostgreTestContext>();
        PostgreTestContext.Annotations.AddRange(modelBuilder.Model.GetAnnotations());

        await context.Database.EnsureCreatedAsync();

        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE outbox_messages");

        await host.StartAsync();
    }

    protected bool TopicExist(string topicName, int timoutMilliseconds = 1000)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafka.BootstrapAddress }).Build();
        var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromMilliseconds(timoutMilliseconds));
        return metadata.Topics.Any(t => t.Topic == topicName);
    }

    protected bool GroupExist(string groupName, int timoutMilliseconds = 1000)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafka.BootstrapAddress }).Build();
        var groups = adminClient.ListGroups(TimeSpan.FromMilliseconds(timoutMilliseconds));
        return groups.Any(g => g.Group == groupName);
    }

    protected void DeleteKafkaTopics(int timoutMilliseconds = 3000)
    {
        DeleteKafkaTopicsAsync(timoutMilliseconds).GetAwaiter().GetResult();
    }

    protected async Task DeleteKafkaTopicsAsync(int timoutMilliseconds = 3000)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafka.BootstrapAddress }).Build();
        var metadata = adminClient.GetMetadata(TimeSpan.FromMilliseconds(timoutMilliseconds));
        var options = new DeleteTopicsOptions
        {
            OperationTimeout = TimeSpan.FromMilliseconds(timoutMilliseconds),
            RequestTimeout = TimeSpan.FromMilliseconds(timoutMilliseconds)
        };
        await adminClient.DeleteTopicsAsync(metadata.Topics.Where(t => !t.Topic.StartsWith("__", StringComparison.InvariantCulture)).Select(t => t.Topic), options);
        metadata = adminClient.GetMetadata(TimeSpan.FromMilliseconds(timoutMilliseconds));
    }

    public void Dispose()
    {
        host?.Dispose();
        DeleteKafkaTopics();
        context?.Dispose();
        PostgreTestContext.Annotations?.Clear();
    }
}
