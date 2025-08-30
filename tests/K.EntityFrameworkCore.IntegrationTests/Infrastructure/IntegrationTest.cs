using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that uses shared TestContainers via fixture
/// </summary>
public abstract class IntegrationTest
{
    private readonly KafkaFixture kafka;
    private readonly PostgreSqlFixture postgreSql;

    private WebApplication host;
    private ModelBuilder internalModelBuilder = new();

    protected WebApplicationBuilder builder;
    protected PostgreTestContext context;
    protected TopicTypeBuilder<MessageType> defaultTopic;
    protected TopicTypeBuilder<MessageTypeB> alternativeTopic;

    
    public IntegrationTest(KafkaFixture kafka, PostgreSqlFixture postgreSql)
    {
        this.kafka = kafka;
        this.postgreSql = postgreSql;

        builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        Environment.SetEnvironmentVariable("ConnectionStrings:kafka", kafka.BootstrapAddress);
        Environment.SetEnvironmentVariable("ConnectionStrings:postgres", postgreSql.Connection);

        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddDbContext<PostgreTestContext>(options => options
            .UseNpgsql(builder.Configuration.GetConnectionString("postgres"))
            .EnableServiceProviderCaching(false)
            .UseKafkaExtensibility(builder.Configuration.GetConnectionString("kafka"))
            .ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>());

        defaultTopic = new TopicTypeBuilder<MessageType>(internalModelBuilder);
        alternativeTopic = new TopicTypeBuilder<MessageTypeB>(internalModelBuilder);
    }

    protected async Task ApplyModelAndStartHostAsync()
    {
        host = builder.Build();

        context = host.Services.GetService<PostgreTestContext>();
        context.Annotations = internalModelBuilder.Model.GetAnnotations();

        await context.Database.EnsureCreatedAsync();
        await host.StartAsync();
    }

    protected bool TopicExist(string topicName, int timoutMilliseconds = 1000)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafka.BootstrapAddress }).Build();
        var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromMilliseconds(timoutMilliseconds));
        return metadata.Topics.Any(t => t.Topic == topicName);
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
        await adminClient.DeleteTopicsAsync(metadata.Topics.Where(t => !t.Topic.StartsWith("__")).Select(t => t.Topic), options);
        metadata = adminClient.GetMetadata(TimeSpan.FromMilliseconds(timoutMilliseconds));
    }
}

public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return new DynamicModelCacheKey(context, designTime, Guid.NewGuid());
    }
}

public class DynamicModelCacheKey(DbContext context, bool designTime, Guid uniqueKey) : ModelCacheKey(context, designTime)
{
    private readonly Guid uniqueKey = uniqueKey;

    public override bool Equals(object obj)
        => obj is DynamicModelCacheKey other
           && base.Equals(other)
           && uniqueKey == other.uniqueKey;

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), uniqueKey);
}