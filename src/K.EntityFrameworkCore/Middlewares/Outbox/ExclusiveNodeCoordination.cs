using Confluent.Kafka;
using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

/// <summary>
/// Coordination strategy that uses a Kafka consumer group with a single-partition
/// coordination topic to elect exactly one leader node. Only the leader processes
/// outbox rows; all other nodes return an empty query from <see cref="ApplyScope"/>.
/// </summary>
/// <typeparam name="TDbContext">The EF Core DbContext type.</typeparam>
internal sealed class ExclusiveNodeCoordination<TDbContext> : IOutboxCoordinationStrategy<TDbContext>, IHostedService, IDisposable
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExclusiveNodeOptions _options;
    private readonly ILogger<ExclusiveNodeCoordination<TDbContext>> _logger;

    private volatile bool _isLeader;
    private CancellationTokenSource? _cts;
    private Task? _consumerLoop;
    private Task? _heartbeatLoop;
    private IConsumer<string, byte[]>? _consumer;
    private IProducer<string, byte[]>? _producer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveNodeCoordination{TDbContext}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider used to resolve <see cref="ClientConfig"/>.</param>
    /// <param name="options">The exclusive-node configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ExclusiveNodeCoordination(
        IServiceProvider serviceProvider,
        IOptions<ExclusiveNodeOptions> options,
        ILogger<ExclusiveNodeCoordination<TDbContext>> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether this node is the current leader.
    /// </summary>
    internal bool IsLeader => _isLeader;

    /// <inheritdoc />
    public IQueryable<OutboxMessage> ApplyScope(IQueryable<OutboxMessage> source)
    {
        return _isLeader ? source : source.Where(_ => false);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting exclusive-node coordination for {DbContext} on topic '{Topic}' with group '{Group}'",
            typeof(TDbContext).Name, _options.TopicName, _options.GroupId);

        var clientConfig = ResolveClientConfig();
        await EnsureCoordinationTopicAsync(clientConfig, cancellationToken);

        var consumerConfig = new ConsumerConfig(clientConfig.ToDictionary())
        {
            GroupId = _options.GroupId,
            EnableAutoCommit = true,
            AutoOffsetReset = AutoOffsetReset.Latest,
            HeartbeatIntervalMs = (int)_options.HeartbeatInterval.TotalMilliseconds,
            SessionTimeoutMs = (int)_options.SessionTimeout.TotalMilliseconds,
        };

        var producerConfig = new ProducerConfig(clientConfig.ToDictionary());

        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
            .SetPartitionsAssignedHandler(OnPartitionsAssigned)
            .SetPartitionsRevokedHandler(OnPartitionsRevoked)
            .Build();

        _producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

        _consumer.Subscribe(_options.TopicName);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumerLoop = Task.Run(() => ConsumerLoop(_cts.Token), CancellationToken.None);
        _heartbeatLoop = Task.Run(() => HeartbeatLoop(_cts.Token), CancellationToken.None);

        _logger.LogInformation("Exclusive-node coordination started for {DbContext}", typeof(TDbContext).Name);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping exclusive-node coordination for {DbContext}", typeof(TDbContext).Name);

        _isLeader = false;

        if (_cts is not null)
        {
            await _cts.CancelAsync();

            try
            {
                if (_consumerLoop is not null)
                    await _consumerLoop;
            }
            catch (OperationCanceledException) { }

            try
            {
                if (_heartbeatLoop is not null)
                    await _heartbeatLoop;
            }
            catch (OperationCanceledException) { }
        }

        _consumer?.Close();
        _logger.LogInformation("Exclusive-node coordination stopped for {DbContext}", typeof(TDbContext).Name);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Dispose();
        _consumer?.Dispose();
        _producer?.Dispose();
    }

    /// <summary>
    /// Callback invoked when partitions are assigned to this consumer.
    /// Sets this node as the leader.
    /// </summary>
    /// <param name="consumer">The Kafka consumer instance.</param>
    /// <param name="partitions">The partitions assigned to this consumer.</param>
    internal void OnPartitionsAssigned(IConsumer<string, byte[]> consumer, List<TopicPartition> partitions)
    {
        _isLeader = true;
        _logger.LogInformation(
            "Partitions assigned — this node is now the LEADER for {DbContext}. Partitions: {Partitions}",
            typeof(TDbContext).Name, string.Join(", ", partitions));
    }

    /// <summary>
    /// Callback invoked when partitions are revoked from this consumer.
    /// Clears leadership from this node.
    /// </summary>
    /// <param name="consumer">The Kafka consumer instance.</param>
    /// <param name="partitions">The partitions revoked from this consumer.</param>
    internal void OnPartitionsRevoked(IConsumer<string, byte[]> consumer, List<TopicPartitionOffset> partitions)
    {
        _isLeader = false;
        _logger.LogWarning(
            "Partitions revoked — this node is NO LONGER the leader for {DbContext}. Partitions: {Partitions}",
            typeof(TDbContext).Name, string.Join(", ", partitions));
    }

    /// <summary>
    /// Background loop that continuously polls the Kafka consumer to keep the consumer
    /// group membership alive. When this node holds the single partition, it is the leader.
    /// </summary>
    private async Task ConsumerLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _consumer!.Consume(ct);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Consume error in exclusive-node coordination for {DbContext}", typeof(TDbContext).Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Consumer loop cancelled for {DbContext}", typeof(TDbContext).Name);
        }
    }

    /// <summary>
    /// Background loop that periodically produces a heartbeat message to the coordination topic.
    /// This ensures there is activity on the topic even if no external producers are writing to it.
    /// </summary>
    private async Task HeartbeatLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var message = new Message<string, byte[]>
                    {
                        Key = Environment.MachineName,
                        Value = []
                    };

                    _producer!.Produce(_options.TopicName, message);
                }
                catch (ProduceException<string, byte[]> ex)
                {
                    _logger.LogWarning(ex, "Heartbeat produce error for {DbContext}", typeof(TDbContext).Name);
                }

                await Task.Delay(_options.HeartbeatInterval, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Heartbeat loop cancelled for {DbContext}", typeof(TDbContext).Name);
        }
    }

    /// <summary>
    /// Resolves the <see cref="ClientConfig"/> from the DbContext infrastructure services.
    /// </summary>
    private ClientConfig ResolveClientConfig()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        return dbContext.GetInfrastructure().GetRequiredService<ClientConfig>();
    }

    /// <summary>
    /// Ensures the coordination topic exists with exactly one partition.
    /// </summary>
    private async Task EnsureCoordinationTopicAsync(ClientConfig clientConfig, CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(clientConfig).Build();

        try
        {
            var metadata = adminClient.GetMetadata(_options.TopicName, TimeSpan.FromSeconds(10));
            var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic == _options.TopicName);

            if (topicMetadata is not null && topicMetadata.Error.Code == ErrorCode.NoError && topicMetadata.Partitions.Count > 0)
            {
                _logger.LogInformation("Coordination topic '{Topic}' already exists with {Partitions} partition(s)",
                    _options.TopicName, topicMetadata.Partitions.Count);
                return;
            }
        }
        catch (KafkaException ex)
        {
            _logger.LogDebug(ex, "Could not retrieve metadata for topic '{Topic}', will attempt to create it", _options.TopicName);
        }

        try
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = _options.TopicName,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ], new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(30),
                OperationTimeout = TimeSpan.FromSeconds(30)
            });

            _logger.LogInformation("Created coordination topic '{Topic}' with 1 partition", _options.TopicName);
        }
        catch (CreateTopicsException ex)
        {
            foreach (var result in ex.Results)
            {
                if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogInformation("Coordination topic '{Topic}' already exists", _options.TopicName);
                }
                else if (result.Error.Code != ErrorCode.NoError)
                {
                    _logger.LogError("Failed to create coordination topic '{Topic}': {Error}", result.Topic, result.Error.Reason);
                    throw;
                }
            }
        }
    }
}
