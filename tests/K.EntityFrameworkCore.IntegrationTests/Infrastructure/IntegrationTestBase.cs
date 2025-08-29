using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Reflection;
using Xunit.Abstractions;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that uses shared TestContainers via fixture
/// </summary>
public abstract class IntegrationTestBase(KafkaFixture kafka, PostgreSqlFixture postgreSql, WebApplicationBuilder builder) : IAsyncDisposable
{
    protected IntegrationTestBase(KafkaFixture kafka, PostgreSqlFixture postgreSql) : this(kafka, postgreSql, CreateDefaultBuilder())
    {
    }

    private WebApplication host;
    private IServiceProvider services;

    protected IServiceProvider Services
    {
        get
        {
            EnsureInitialized();
            return services;
        }
    }

    protected WebApplicationBuilder Builder { get; } = builder;

    private static WebApplicationBuilder CreateDefaultBuilder()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { ApplicationName = "TestHost" });
        builder.WebHost.UseTestServer();

        return builder;
    }

    protected DbContext Context
    {
        get
        {
            EnsureInitialized();
            return field;
        }
        private set => field = value;
    }

    protected void OnModelCreating(Action<ModelBuilder> action)
    {
        var contextType = Builder.Services.SingleOrDefault(sd => typeof(DbContext).IsAssignableFrom(sd.ServiceType))?.ServiceType
            ?? throw new InvalidOperationException("No DbContext registered in the service collection. Did you forget to add one in your test?");

        contextType.GetProperty(nameof(IModelCreatingExternal.OnModelCreatingExternal), BindingFlags.Public | BindingFlags.Static).SetValue(null, action);
    }

    protected bool TopicExist(string topicName, int timoutMilliseconds = 1000)
    {
        using var adminClient = new Confluent.Kafka.AdminClientBuilder(new Confluent.Kafka.AdminClientConfig { BootstrapServers = kafka.BootstrapAddress }).Build();
        var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromMilliseconds(timoutMilliseconds));
        return metadata.Topics.Any(t => t.Topic == topicName);
    }

    public async ValueTask DisposeAsync()
    {
        await host.StopAsync();
        await host.DisposeAsync();
    }

    private void EnsureInitialized()
    {
        if (Builder.Services.IsReadOnly)
            return;

        Environment.SetEnvironmentVariable("ConnectionStrings:kafka", kafka.BootstrapAddress);
        Environment.SetEnvironmentVariable("ConnectionStrings:postgres", postgreSql.Connection);
        Builder.Configuration.AddEnvironmentVariables();

        this.host = Builder.Build();
        this.host.Start();
        this.services = host.Services;

        var contextType = Builder.Services.SingleOrDefault(sd => typeof(DbContext).IsAssignableFrom(sd.ServiceType))?.ServiceType
                ?? throw new InvalidOperationException("No DbContext registered in the service collection. Did you forget to add one in your test?");

        Context = services.GetService(contextType) as DbContext;
        Context.Database.EnsureCreated();
    }
}

internal static class ModelBuilderExtensions
{
    public static WebApplicationBuilder AddSingleTopicDbContextWithKafka(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<SingleTopicDbContext>(options => options
                        .UseNpgsql(builder.Configuration.GetConnectionString("postgres"))
                        .UseKafkaExtensibility(builder.Configuration.GetConnectionString("kafka")));

        return builder;
    }
}