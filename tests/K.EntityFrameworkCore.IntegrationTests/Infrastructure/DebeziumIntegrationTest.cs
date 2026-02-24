using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for Debezium CDC integration tests, mirroring the structure of
/// <see cref="IntegrationTest"/> but targeting the Testcontainers-backed
/// <see cref="DebeziumFixture"/> (Postgres + Kafka + Kafka Connect).
/// </summary>
[SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Properties has overheads for tests")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Is necessary")]
public abstract class DebeziumIntegrationTest : IDisposable
{
    private readonly DebeziumFixture _debezium;
    private readonly ModelBuilder internalModelBuilder = new();
    private WebApplication host;
    protected WebApplicationBuilder builder;
    protected DebeziumTestContext context;
    protected TopicTypeBuilder<DebeziumOrderPlaced> orderEventTopic;

    protected DebeziumIntegrationTest(DebeziumFixture debezium)
    {
        _debezium = debezium;
        builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        Environment.SetEnvironmentVariable("ConnectionStrings:kafka", debezium.BootstrapServers);
        Environment.SetEnvironmentVariable("ConnectionStrings:postgres", debezium.PostgresConnection);

        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddDbContext<DebeziumTestContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("postgres"))
                   .EnableServiceProviderCaching(false)
                   .UseKafkaExtensibility(builder.Configuration.GetConnectionString("kafka")));

        orderEventTopic = new TopicTypeBuilder<DebeziumOrderPlaced>(internalModelBuilder);
    }

    protected async Task StartHostAsync()
    {
        host = builder.Build();

        context = host.Services.GetService<DebeziumTestContext>();
        DebeziumTestContext.Annotations.AddRange(internalModelBuilder.Model.GetAnnotations());

        await context.Database.EnsureCreatedAsync();
        await _debezium.EnsureConnectorRegisteredAsync();
        await host.StartAsync();
    }

    public void Dispose()
    {
        host?.StopAsync();
        (host as IDisposable)?.Dispose();
        context?.Dispose();
    }
}
