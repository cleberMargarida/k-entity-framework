using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Polly;
using Polly.Retry;
using Testcontainers.Kafka;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Containers;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up a full Debezium CDC stack using Testcontainers:
/// PostgreSQL (wal_level=logical), Kafka (KRaft via Testcontainers), and
/// Kafka Connect (custom SMT image) — all joined on a shared Docker network.
/// The Debezium connector is registered lazily (after the outbox table exists).
/// </summary>
public class DebeziumFixture : IAsyncLifetime
{
    private INetwork _network;
    private IContainer _postgres;
    private KafkaContainer _kafka;
    private IContainer _kafkaConnect;

    public string BootstrapServers => _kafka.GetBootstrapAddress();
    public string PostgresConnection =>
        $"Host=localhost;Port={_postgres.GetMappedPublicPort(5432)};Database=postgres;Username=postgres;Password=postgres";

    public async ValueTask InitializeAsync()
    {
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        _postgres = new ContainerBuilder()
            .WithImage("postgres:16.4")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "postgres")
            .WithCommand("postgres", "-c", "wal_level=logical")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilCommandIsCompleted("pg_isready", "-U", "postgres"))
            .Build();

        _kafka = new KafkaBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases("kafka")
            .Build();

        // Start Postgres and Kafka in parallel; Connect depends on both.
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        _kafkaConnect = new ContainerBuilder()
            .WithImage("k-entity-framework/kafka-connect-smt")
            .WithNetwork(_network)
            .WithNetworkAliases("kafka-connect")
            .WithPortBinding(8083, true)
            .WithEnvironment("BOOTSTRAP_SERVERS", "kafka:9093")
            .WithEnvironment("GROUP_ID", "debezium-cluster")
            .WithEnvironment("CONFIG_STORAGE_TOPIC", "connect-configs")
            .WithEnvironment("OFFSET_STORAGE_TOPIC", "connect-offsets")
            .WithEnvironment("STATUS_STORAGE_TOPIC", "connect-status")
            .WithEnvironment("CONFIG_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("OFFSET_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("STATUS_STORAGE_REPLICATION_FACTOR", "1")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(8083).ForPath("/connectors")))
            .Build();

        await _kafkaConnect.StartAsync();
    }

    /// <summary>
    /// Registers the Debezium connector if not already registered,
    /// then waits until all connector tasks reach RUNNING state.
    /// Must be called after the <c>outbox_messages</c> table exists.
    /// </summary>
    public async Task EnsureConnectorRegisteredAsync()
    {
        using var http = new HttpClient();
        var port = _kafkaConnect.GetMappedPublicPort(8083);
        var baseUrl = $"http://localhost:{port}";
        const string connectorName = "postgres-outbox-connector";

        // Idempotent — skip registration if connector already exists
        var check = await http.GetAsync($"{baseUrl}/connectors/{connectorName}");
        if (!check.IsSuccessStatusCode)
        {
            var config = new JsonObject
            {
                ["name"] = connectorName,
                ["config"] = new JsonObject
                {
                    ["connector.class"] = "io.debezium.connector.postgresql.PostgresConnector",
                    ["database.hostname"] = "postgres",
                    ["database.port"] = "5432",
                    ["database.user"] = "postgres",
                    ["database.password"] = "postgres",
                    ["database.dbname"] = "postgres",
                    ["database.server.name"] = "pgserver1",
                    ["table.include.list"] = "public.outbox_messages",
                    ["publication.name"] = "dbz_publication",
                    ["plugin.name"] = "pgoutput",
                    ["topic.prefix"] = "pgserver1",
                    ["transforms"] = "outbox,expandHeaders",
                    ["transforms.outbox.type"] = "io.debezium.transforms.outbox.EventRouter",
                    ["transforms.outbox.table.field.event.id"] = "Id",
                    ["transforms.outbox.table.field.event.key"] = "AggregateId",
                    ["transforms.outbox.table.field.event.payload"] = "Payload",
                    ["transforms.outbox.route.by.field"] = "Topic",
                    ["transforms.outbox.route.topic.regex"] = "(.*)",
                    ["transforms.outbox.route.topic.replacement"] = "$1",
                    ["transforms.outbox.table.expand.json.payload"] = "false",
                    ["transforms.outbox.table.fields.additional.placement"] = "Headers:header:__debezium.outbox.headers",
                    ["transforms.expandHeaders.type"] = "k.entityframework.kafka.connect.transforms.HeaderJsonExpander",
                    ["key.converter"] = "org.apache.kafka.connect.storage.StringConverter",
                    ["value.converter"] = "org.apache.kafka.connect.converters.ByteArrayConverter",
                    ["slot.name"] = "dbz_outbox_slot",
                    ["tombstones.on.delete"] = "false",
                    ["errors.tolerance"] = "all",
                    ["errors.log.enable"] = "true",
                    ["errors.log.include.messages"] = "true"
                }
            };

            var response = await http.PostAsJsonAsync($"{baseUrl}/connectors", config);
            response.EnsureSuccessStatusCode();
        }

        // Wait until connector and all tasks are in RUNNING state
        await WaitForConnectorRunningAsync(http, baseUrl, connectorName);
    }

    /// <summary>
    /// Polls the connector status endpoint until the connector and all its
    /// tasks report a RUNNING state (or until timeout).
    /// </summary>
    private static async Task WaitForConnectorRunningAsync(
        HttpClient http, string baseUrl, string connectorName, int timeoutMs = 60_000)
    {
        var maxAttempts = (int)Math.Ceiling((double)timeoutMs / 500);

        var pipeline = new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                MaxRetryAttempts = maxAttempts,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<bool>()
                    .Handle<HttpRequestException>()
                    .HandleResult(false)
            })
            .Build();

        var running = await pipeline.ExecuteAsync(async ct =>
        {
            var statusResponse = await http.GetAsync($"{baseUrl}/connectors/{connectorName}/status", ct);
            if (!statusResponse.IsSuccessStatusCode)
                return false;

            var status = await statusResponse.Content.ReadFromJsonAsync<JsonObject>(ct);
            var connectorState = status?["connector"]?["state"]?.GetValue<string>();

            if (connectorState == "FAILED")
            {
                var trace = status?["connector"]?["trace"]?.GetValue<string>() ?? "unknown";
                throw new InvalidOperationException(
                    $"Debezium connector '{connectorName}' is in FAILED state: {trace}");
            }

            var tasks = status?["tasks"]?.AsArray();
            return connectorState == "RUNNING" &&
                   tasks is { Count: > 0 } &&
                   tasks.All(t => t?["state"]?.GetValue<string>() == "RUNNING");
        }, CancellationToken.None);

        if (!running)
            throw new TimeoutException(
                $"Debezium connector '{connectorName}' did not reach RUNNING state within {timeoutMs}ms.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_kafkaConnect is not null) await _kafkaConnect.DisposeAsync();
        if (_kafka is not null) await _kafka.DisposeAsync();
        if (_postgres is not null) await _postgres.DisposeAsync();
        if (_network is not null) await _network.DeleteAsync();
    }
}
