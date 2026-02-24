# Kafka Performance & Monitoring

This guide covers performance tuning, monitoring, debugging, and best practices for Kafka clients in K-Entity-Framework.

For connection and authentication configuration, see [Kafka Configuration](kafka-configuration.md) and [Kafka Security](kafka-security.md).

## Performance Tuning

### High Throughput

```csharp
// Producer optimizations
kafka.Producer.BatchSize = 65536;                    // 64 KB batches
kafka.Producer.LingerMs = 20;                        // Wait up to 20 ms for batching
kafka.Producer.CompressionType = CompressionType.Lz4; // Fast compression
kafka.Producer.QueueBufferingMaxMessages = 1000000;  // Large in-memory buffer
kafka.Producer.QueueBufferingMaxKbytes = 2097152;    // 2 GB buffer cap

// Consumer optimizations
kafka.Consumer.FetchMinBytes = 50000;             // Fetch at least 50 KB
kafka.Consumer.FetchMaxWaitMs = 50;
kafka.Consumer.MaxPartitionFetchBytes = 10485760; // 10 MB per partition
```

### Low Latency

```csharp
// Producer — send immediately, no batching
kafka.Producer.LingerMs = 0;
kafka.Producer.BatchSize = 0;
kafka.Producer.QueueBufferingMaxMs = 0;

// Consumer — fetch any available data without waiting
kafka.Consumer.FetchMinBytes = 1;
kafka.Consumer.FetchMaxWaitMs = 1;
```

## Monitoring

### Enable Statistics

```csharp
kafka.StatisticsIntervalMs = 5000;    // Emit statistics every 5 seconds
kafka.EnableDeliveryReports = true;   // Enable producer delivery reports
```

### Debug Logging

```csharp
kafka.LogLevel = 7; // Debug level (0–7; 7 is most verbose)
kafka.Debug = "broker,topic,msg";

// Available debug categories:
// broker, topic, msg, protocol, cgrp, security, fetch, interceptor,
// plugin, assignor, conf
```

### Health Checks

```csharp
public class KafkaHealthCheck : IHealthCheck
{
    private readonly KafkaClientSettings _settings;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(_settings.ClientConfig).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            return HealthCheckResult.Healthy($"Kafka healthy. Brokers: {metadata.Brokers.Count}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Kafka unhealthy: {ex.Message}");
        }
    }
}

// Register
builder.Services.AddHealthChecks()
       .AddCheck<KafkaHealthCheck>("kafka");
```

## Best Practices

### Start with Defaults

Begin with sensible defaults and tune based on your specific requirements:

```csharp
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.Consumer.EnableAutoCommit = false;
kafka.Consumer.IsolationLevel = IsolationLevel.ReadCommitted;
```

### Environment-Specific Configuration

```csharp
var environment = builder.Environment.EnvironmentName;
kafka.ClientId = $"my-app-{environment}";
kafka.Consumer.GroupId = $"my-group-{environment}";

if (environment == "Production")
{
    kafka.LogLevel = 3; // Error level only
    kafka.Producer.Acks = Acks.All;
}
else
{
    kafka.LogLevel = 6; // Info level
    kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
}
```

### Monitor Performance

```csharp
kafka.StatisticsIntervalMs = 60000; // Statistics every minute
kafka.EnableDeliveryReports = true;
```

## Related

- [Kafka Configuration](kafka-configuration.md) — basic, producer, and consumer settings
- [Kafka Security](kafka-security.md) — TLS, SASL, Kerberos, and cloud providers
