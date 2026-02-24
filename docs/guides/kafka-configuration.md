# Kafka Configuration

This guide covers configuring Kafka client, producer, and consumer settings in K-Entity-Framework.

- [Security (TLS, SASL, Kerberos, cloud providers)](kafka-security.md)
- [Performance tuning, monitoring, and health checks](kafka-performance.md)

## Overview

K-Entity-Framework provides multiple levels of configuration:

1. **Client-level configuration** — Global settings that apply to all producers and consumers
2. **Producer-specific configuration** — Settings that only apply to message production
3. **Consumer-specific configuration** — Settings that only apply to message consumption
4. **Topic-specific configuration** — Settings that apply to specific message types

## Basic Configuration

### Simple Setup

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseKafkaExtensibility(kafka =>
           {
               kafka.BootstrapServers = "localhost:9092";
               kafka.ClientId = "my-application";
           }));
```

### Development Configuration

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseKafkaExtensibility(kafka =>
           {
               kafka.BootstrapServers = "localhost:9092";
               kafka.ClientId = "my-dev-app";

               // Development-friendly settings
               kafka.Consumer.GroupId = "dev-consumer-group";
               kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
               kafka.Producer.Acks = Acks.Leader; // Faster for development

               // Enable detailed logging
               kafka.LogLevel = 7; // Debug level
               kafka.Debug = "broker,topic,msg";
           }));
```

## Client-Level Configuration

### Connection Settings

```csharp
kafka.BootstrapServers = "broker1:9092,broker2:9092,broker3:9092";
kafka.ClientId = "my-application";

// Timeout settings
kafka.SocketTimeoutMs = 60000;
kafka.RequestTimeoutMs = 30000;
kafka.MetadataMaxAgeMs = 300000;

// Connection limits
kafka.MaxInFlight = 5;
kafka.MessageMaxBytes = 1000000;

// Retry settings
kafka.RetryBackoffMs = 100;
kafka.ReconnectBackoffMs = 50;
kafka.ReconnectBackoffMaxMs = 10000;
```

For security and authentication settings (TLS, SASL, Kerberos, cloud providers), see [Kafka Security](kafka-security.md).

## Producer Configuration

### Basic Producer Settings

```csharp
kafka.Producer.Acks = Acks.All;          // Wait for all replicas
kafka.Producer.EnableIdempotence = true; // Exactly-once semantics
kafka.Producer.MaxInFlight = 5;
kafka.Producer.Retries = int.MaxValue;
kafka.Producer.RetryBackoffMs = 100;
```

### Performance Optimization

```csharp
// Batching
kafka.Producer.BatchSize = 16384;                          // 16 KB batch size
kafka.Producer.LingerMs = 5.0;                             // Wait up to 5 ms to fill batch
kafka.Producer.QueueBufferingMaxMessages = 100000;
kafka.Producer.QueueBufferingMaxKbytes = 1048576;          // 1 GB memory buffer

// Compression
kafka.Producer.CompressionType = CompressionType.Snappy;   // Fast compression

// Timeouts
kafka.Producer.MessageTimeoutMs = 300000;                  // 5 minutes total
kafka.Producer.RequestTimeoutMs = 30000;
```

### Delivery Guarantees

```csharp
// Exactly-once delivery (recommended for production)
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.Producer.MaxInFlight = 5;

// At-least-once delivery
kafka.Producer.Acks = Acks.Leader;
kafka.Producer.EnableIdempotence = false;

// Fire-and-forget (best performance, possible data loss)
kafka.Producer.Acks = Acks.None;
kafka.Producer.EnableDeliveryReports = false;
```

### Transactional Configuration

```csharp
kafka.Producer.TransactionalId = "my-transactional-producer";
kafka.Producer.TransactionTimeoutMs = 60000;
kafka.Producer.EnableIdempotence = true; // Required for transactions
```

## Consumer Configuration

### Basic Consumer Settings

```csharp
kafka.Consumer.GroupId = "my-consumer-group";
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
kafka.Consumer.EnableAutoCommit = false; // Manual commit for better control
kafka.Consumer.IsolationLevel = IsolationLevel.ReadCommitted;
```

### Session and Heartbeat Settings

```csharp
kafka.Consumer.SessionTimeoutMs = 10000;   // 10 seconds
kafka.Consumer.HeartbeatIntervalMs = 3000; // Should be ~1/3 of session timeout
kafka.Consumer.MaxPollIntervalMs = 300000; // 5 minutes max processing time
```

### Fetch Settings

```csharp
kafka.Consumer.FetchMinBytes = 1;
kafka.Consumer.FetchMaxBytes = 52428800;          // 50 MB max fetch size
kafka.Consumer.MaxPartitionFetchBytes = 1048576;  // 1 MB per partition
kafka.Consumer.FetchMaxWaitMs = 500;
```

### Partition Assignment

```csharp
// Cooperative sticky (recommended — minimizes rebalancing)
kafka.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky;

// Round-robin
kafka.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.RoundRobin;
```

### Auto-Commit

```csharp
// Auto-commit (simpler, less control)
kafka.Consumer.EnableAutoCommit = true;
kafka.Consumer.AutoCommitIntervalMs = 5000;

// Manual commit (recommended)
kafka.Consumer.EnableAutoCommit = false;
```

## Environment-Specific Configuration

### Development

```csharp
kafka.BootstrapServers = "localhost:9092";
kafka.ClientId = "dev-client";
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
kafka.Producer.Acks = Acks.Leader;
kafka.Producer.LingerMs = 0;
kafka.LogLevel = 7;
kafka.Debug = "broker,topic,msg";
```

### Staging

```csharp
kafka.BootstrapServers = "staging-kafka:9092";
kafka.ClientId = "staging-client";
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Latest;
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.LogLevel = 5; // Notice level
```

### Production

```csharp
kafka.BootstrapServers = "prod-kafka1:9092,prod-kafka2:9092,prod-kafka3:9092";
kafka.ClientId = "prod-client-v1.0";
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.Producer.Retries = int.MaxValue;
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Latest;
kafka.Consumer.EnableAutoCommit = false;
kafka.Consumer.SessionTimeoutMs = 30000;
kafka.LogLevel = 3; // Error level only

// Always configure security in production — see kafka-security.md
```

## Configuration from appsettings.json

### Configuration Structure

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ClientId": "my-application",
    "Producer": {
      "Acks": "All",
      "EnableIdempotence": true,
      "BatchSize": 16384,
      "LingerMs": 5.0,
      "CompressionType": "Snappy"
    },
    "Consumer": {
      "GroupId": "my-consumer-group",
      "AutoOffsetReset": "Earliest",
      "EnableAutoCommit": false,
      "SessionTimeoutMs": 10000
    }
  }
}
```

### Configuration Binding

```csharp
public class KafkaSettings
{
    public string BootstrapServers { get; set; }
    public string ClientId { get; set; }
    public ProducerSettings Producer { get; set; } = new();
    public ConsumerSettings Consumer { get; set; } = new();
}

public class ProducerSettings
{
    public Acks Acks { get; set; }
    public bool EnableIdempotence { get; set; }
    public int BatchSize { get; set; }
    public double LingerMs { get; set; }
    public CompressionType CompressionType { get; set; }
}

public class ConsumerSettings
{
    public string GroupId { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; }
    public bool EnableAutoCommit { get; set; }
    public int SessionTimeoutMs { get; set; }
}

// Usage
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>();

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseKafkaExtensibility(kafka =>
           {
               kafka.BootstrapServers = kafkaSettings.BootstrapServers;
               kafka.ClientId = kafkaSettings.ClientId;
               kafka.Producer.Acks = kafkaSettings.Producer.Acks;
               kafka.Producer.EnableIdempotence = kafkaSettings.Producer.EnableIdempotence;
               kafka.Producer.BatchSize = kafkaSettings.Producer.BatchSize;
               kafka.Producer.LingerMs = kafkaSettings.Producer.LingerMs;
               kafka.Producer.CompressionType = kafkaSettings.Producer.CompressionType;
               kafka.Consumer.GroupId = kafkaSettings.Consumer.GroupId;
               kafka.Consumer.AutoOffsetReset = kafkaSettings.Consumer.AutoOffsetReset;
               kafka.Consumer.EnableAutoCommit = kafkaSettings.Consumer.EnableAutoCommit;
               kafka.Consumer.SessionTimeoutMs = kafkaSettings.Consumer.SessionTimeoutMs;
           }));
```

## Advanced Configuration

### Custom Configuration Provider

```csharp
public class CustomKafkaConfigurationProvider
{
    public void ConfigureKafka(KafkaClientSettings kafka)
    {
        kafka.BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
        kafka.ClientId = Environment.GetEnvironmentVariable("KAFKA_CLIENT_ID");
        kafka.SaslUsername = GetSecretValue("kafka-username");
        kafka.SaslPassword = GetSecretValue("kafka-password");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            ConfigureProductionSettings(kafka);
        else
            ConfigureDevelopmentSettings(kafka);
    }

    private void ConfigureProductionSettings(KafkaClientSettings kafka)
    {
        kafka.Producer.Acks = Acks.All;
        kafka.Producer.EnableIdempotence = true;
        kafka.Consumer.EnableAutoCommit = false;
        kafka.LogLevel = 3;
    }

    private void ConfigureDevelopmentSettings(KafkaClientSettings kafka)
    {
        kafka.Producer.Acks = Acks.Leader;
        kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
        kafka.LogLevel = 7;
        kafka.Debug = "broker,topic,msg";
    }
}

// Usage
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseKafkaExtensibility(kafka =>
           {
               var configProvider = new CustomKafkaConfigurationProvider();
               configProvider.ConfigureKafka(kafka);
           }));
```

### Accessing the Underlying ClientConfig

```csharp
kafka.ClientConfig.Set("custom.property", "custom.value");
var underlyingConfig = kafka.ClientConfig;
underlyingConfig.SslCaLocation = "/path/to/ca.pem";
```

## Related

- [Kafka Security](kafka-security.md) — TLS, SASL, Kerberos, Confluent Cloud, MSK, Azure Event Hubs
- [Kafka Performance & Monitoring](kafka-performance.md) — high-throughput, low-latency, health checks, debug logging
- [Connection Management](connection-management.md) — producer/consumer singleton patterns

