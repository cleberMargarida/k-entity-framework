# Kafka Configuration

This guide provides comprehensive information on configuring Kafka settings in K-Entity-Framework. The framework provides a strongly-typed configuration API that covers all aspects of Kafka client configuration.

## Overview

K-Entity-Framework provides multiple levels of configuration:

1. **Client-level configuration** - Global settings that apply to all producers and consumers
2. **Producer-specific configuration** - Settings that only apply to message production
3. **Consumer-specific configuration** - Settings that only apply to message consumption
4. **Topic-specific configuration** - Settings that apply to specific message types

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

### Security Configuration

#### SSL/TLS Configuration

```csharp
kafka.SecurityProtocol = SecurityProtocol.Ssl;
kafka.SslCaLocation = "/path/to/ca-cert.pem";
kafka.SslCertificateLocation = "/path/to/client-cert.pem";
kafka.SslKeyLocation = "/path/to/client-key.pem";
kafka.SslKeyPassword = "key-password";
kafka.EnableSslCertificateVerification = true;
kafka.SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https;
```

#### SASL Authentication

```csharp
// SASL/PLAIN
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Plain;
kafka.SaslUsername = "my-username";
kafka.SaslPassword = "my-password";

// SASL/SCRAM
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.ScramSha256;
kafka.SaslUsername = "my-username";
kafka.SaslPassword = "my-password";

// SASL/OAUTHBEARER
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.OAuthBearer;
kafka.SaslOauthbearerConfig = "principalClaimName=sub";
```

#### Kerberos Authentication

```csharp
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Gssapi;
kafka.SaslKerberosServiceName = "kafka";
kafka.SaslKerberosPrincipal = "client@REALM.COM";
kafka.SaslKerberosKeytab = "/path/to/client.keytab";
```

## Producer Configuration

### Basic Producer Settings

```csharp
kafka.Producer.Acks = Acks.All; // Wait for all replicas
kafka.Producer.EnableIdempotence = true; // Exactly-once semantics
kafka.Producer.MaxInFlight = 5; // Allow up to 5 unacknowledged requests
kafka.Producer.Retries = int.MaxValue; // Retry indefinitely
kafka.Producer.RetryBackoffMs = 100; // Wait 100ms between retries
```

### Performance Optimization

```csharp
// Batching settings
kafka.Producer.BatchSize = 16384; // 16KB batch size
kafka.Producer.LingerMs = 5.0; // Wait up to 5ms to fill batch
kafka.Producer.QueueBufferingMaxMessages = 100000; // Buffer up to 100k messages
kafka.Producer.QueueBufferingMaxKbytes = 1048576; // 1GB memory buffer

// Compression
kafka.Producer.CompressionType = CompressionType.Snappy; // Fast compression
// kafka.Producer.CompressionType = CompressionType.Lz4; // Alternative
// kafka.Producer.CompressionType = CompressionType.Gzip; // Better compression ratio

// Timeout settings
kafka.Producer.MessageTimeoutMs = 300000; // 5 minutes total timeout
kafka.Producer.RequestTimeoutMs = 30000; // 30 seconds per request
```

### Transactional Configuration

```csharp
kafka.Producer.TransactionalId = "my-transactional-producer";
kafka.Producer.TransactionTimeoutMs = 60000; // 1 minute transaction timeout
kafka.Producer.EnableIdempotence = true; // Required for transactions
```

### Delivery Guarantees

```csharp
// At-least-once delivery (default)
kafka.Producer.Acks = Acks.Leader;
kafka.Producer.EnableIdempotence = false;

// Exactly-once delivery
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.Producer.MaxInFlight = 5;

// Fire-and-forget (best performance, possible data loss)
kafka.Producer.Acks = Acks.None;
kafka.Producer.EnableDeliveryReports = false;
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
kafka.Consumer.SessionTimeoutMs = 10000; // 10 seconds
kafka.Consumer.HeartbeatIntervalMs = 3000; // 3 seconds (1/3 of session timeout)
kafka.Consumer.MaxPollIntervalMs = 300000; // 5 minutes max processing time
```

### Fetch Settings

```csharp
kafka.Consumer.FetchMinBytes = 1; // Minimum bytes to fetch
kafka.Consumer.FetchMaxBytes = 52428800; // 50MB max fetch size
kafka.Consumer.MaxPartitionFetchBytes = 1048576; // 1MB per partition
kafka.Consumer.FetchMaxWaitMs = 500; // Wait up to 500ms for min bytes
```

### Partition Assignment

```csharp
// Cooperative sticky assignment (recommended)
kafka.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky;

// Range assignment
kafka.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.Range;

// Round-robin assignment
kafka.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.RoundRobin;

// Sticky assignment
kafka.Consumer.PartitionAssignmentStrategy = PartitionAssignmentStrategy.Sticky;
```

### Auto-Commit Configuration

```csharp
// Enable auto-commit (simpler but less control)
kafka.Consumer.EnableAutoCommit = true;
kafka.Consumer.AutoCommitIntervalMs = 5000; // Commit every 5 seconds

// Disable auto-commit (manual control)
kafka.Consumer.EnableAutoCommit = false;
// Handle commits manually in your application code
```

## Environment-Specific Configuration

### Development Environment

```csharp
kafka.BootstrapServers = "localhost:9092";
kafka.ClientId = "dev-client";

// Fast feedback for development
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
kafka.Producer.Acks = Acks.Leader; // Faster, less durability
kafka.Producer.LingerMs = 0; // No batching delay

// Detailed logging
kafka.LogLevel = 7;
kafka.Debug = "broker,topic,msg";
```

### Staging Environment

```csharp
kafka.BootstrapServers = "staging-kafka:9092";
kafka.ClientId = "staging-client";

// Production-like settings
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Latest;
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;

// Moderate logging
kafka.LogLevel = 5; // Notice level
```

### Production Environment

```csharp
kafka.BootstrapServers = "prod-kafka1:9092,prod-kafka2:9092,prod-kafka3:9092";
kafka.ClientId = "prod-client-v1.0";

// Maximum durability and reliability
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.Producer.Retries = int.MaxValue;

// Conservative consumer settings
kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Latest;
kafka.Consumer.EnableAutoCommit = false; // Manual commit control
kafka.Consumer.SessionTimeoutMs = 30000; // Longer timeout for stability

// Security
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.ScramSha256;
kafka.SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME");
kafka.SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");

// Minimal logging
kafka.LogLevel = 3; // Error level only
```

## Cloud Provider Configurations

### Confluent Cloud

```csharp
kafka.BootstrapServers = "pkc-xxxxx.region.provider.confluent.cloud:9092";
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Plain;
kafka.SaslUsername = "your-api-key";
kafka.SaslPassword = "your-api-secret";

// Optimize for cloud
kafka.Producer.CompressionType = CompressionType.Snappy;
kafka.Producer.BatchSize = 32768; // Larger batches for network efficiency
kafka.Producer.LingerMs = 10; // Allow time for batching
```

### Amazon MSK

```csharp
kafka.BootstrapServers = "b-1.msk-cluster.xxxxx.region.amazonaws.com:9092";

// MSK with IAM authentication
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.OAuthBearer;
kafka.SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc;
kafka.SaslOauthbearerClientId = "your-client-id";
kafka.SaslOauthbearerClientSecret = "your-client-secret";
kafka.SaslOauthbearerTokenEndpointUrl = "https://sts.region.amazonaws.com/";
```

### Azure Event Hubs

```csharp
kafka.BootstrapServers = "your-namespace.servicebus.windows.net:9093";
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Plain;
kafka.SaslUsername = "$ConnectionString";
kafka.SaslPassword = "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key";

// Event Hubs specific optimizations
kafka.Producer.MessageMaxBytes = 1000000; // 1MB max message size
kafka.Consumer.FetchMaxBytes = 1048576; // 1MB fetch size
```

## Configuration from appsettings.json

### Configuration Structure

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ClientId": "my-application",
    "SecurityProtocol": "SaslSsl",
    "SaslMechanism": "Plain",
    "SaslUsername": "username",
    "SaslPassword": "password",
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
    public SecurityProtocol SecurityProtocol { get; set; }
    public SaslMechanism SaslMechanism { get; set; }
    public string SaslUsername { get; set; }
    public string SaslPassword { get; set; }
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

// In Startup.cs or Program.cs
var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>();

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseKafkaExtensibility(kafka =>
           {
               kafka.BootstrapServers = kafkaSettings.BootstrapServers;
               kafka.ClientId = kafkaSettings.ClientId;
               kafka.SecurityProtocol = kafkaSettings.SecurityProtocol;
               kafka.SaslMechanism = kafkaSettings.SaslMechanism;
               kafka.SaslUsername = kafkaSettings.SaslUsername;
               kafka.SaslPassword = kafkaSettings.SaslPassword;
               
               // Producer settings
               kafka.Producer.Acks = kafkaSettings.Producer.Acks;
               kafka.Producer.EnableIdempotence = kafkaSettings.Producer.EnableIdempotence;
               kafka.Producer.BatchSize = kafkaSettings.Producer.BatchSize;
               kafka.Producer.LingerMs = kafkaSettings.Producer.LingerMs;
               kafka.Producer.CompressionType = kafkaSettings.Producer.CompressionType;
               
               // Consumer settings
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
        // Load from environment variables
        kafka.BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
        kafka.ClientId = Environment.GetEnvironmentVariable("KAFKA_CLIENT_ID");
        
        // Load from Azure Key Vault, AWS Secrets Manager, etc.
        kafka.SaslUsername = GetSecretValue("kafka-username");
        kafka.SaslPassword = GetSecretValue("kafka-password");
        
        // Environment-specific settings
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
        {
            ConfigureProductionSettings(kafka);
        }
        else
        {
            ConfigureDevelopmentSettings(kafka);
        }
    }
    
    private void ConfigureProductionSettings(KafkaClientSettings kafka)
    {
        kafka.Producer.Acks = Acks.All;
        kafka.Producer.EnableIdempotence = true;
        kafka.Consumer.EnableAutoCommit = false;
        kafka.LogLevel = 3; // Error level
    }
    
    private void ConfigureDevelopmentSettings(KafkaClientSettings kafka)
    {
        kafka.Producer.Acks = Acks.Leader;
        kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
        kafka.LogLevel = 7; // Debug level
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

### Accessing Underlying Configuration

```csharp
kafka.ClientConfig.Set("custom.property", "custom.value");

// Access the underlying Confluent.Kafka ClientConfig
var underlyingConfig = kafka.ClientConfig;
underlyingConfig.BootstrapServers = "localhost:9092";
underlyingConfig.SslCaLocation = "/path/to/ca.pem";
```

## Monitoring and Debugging

### Enable Statistics

```csharp
kafka.StatisticsIntervalMs = 5000; // Emit statistics every 5 seconds
kafka.EnableDeliveryReports = true; // Enable producer delivery reports
```

### Debug Logging

```csharp
kafka.LogLevel = 7; // Debug level (0-7, where 7 is most verbose)
kafka.Debug = "broker,topic,msg"; // Debug categories

// Available debug categories:
// - broker: Broker communication
// - topic: Topic metadata
// - msg: Message handling  
// - protocol: Protocol details
// - cgrp: Consumer group
// - security: Security/authentication
// - fetch: Message fetching
// - interceptor: Interceptors
// - plugin: Plugins
// - assignor: Partition assignment
// - conf: Configuration
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
            
            return HealthCheckResult.Healthy($"Kafka cluster healthy. Brokers: {metadata.Brokers.Count}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Kafka cluster unhealthy: {ex.Message}");
        }
    }
}

// Register health check
builder.Services.AddHealthChecks()
       .AddCheck<KafkaHealthCheck>("kafka");
```

## Performance Tuning

### High Throughput Configuration

```csharp
// Producer optimizations
kafka.Producer.BatchSize = 65536; // 64KB batches
kafka.Producer.LingerMs = 20; // Wait up to 20ms for batching
kafka.Producer.CompressionType = CompressionType.Lz4; // Fast compression
kafka.Producer.QueueBufferingMaxMessages = 1000000; // Large buffer
kafka.Producer.QueueBufferingMaxKbytes = 2097152; // 2GB buffer

// Consumer optimizations
kafka.Consumer.FetchMinBytes = 50000; // Fetch at least 50KB
kafka.Consumer.FetchMaxWaitMs = 50; // Don't wait too long
kafka.Consumer.MaxPartitionFetchBytes = 10485760; // 10MB per partition
```

### Low Latency Configuration

```csharp
// Producer optimizations
kafka.Producer.LingerMs = 0; // Send immediately
kafka.Producer.BatchSize = 0; // No batching
kafka.Producer.QueueBufferingMaxMs = 0; // No queuing delay

// Consumer optimizations
kafka.Consumer.FetchMinBytes = 1; // Fetch any available data
kafka.Consumer.FetchMaxWaitMs = 1; // Minimal wait time
```

## Best Practices

### 1. Start with Defaults

Begin with sensible defaults and tune based on your specific requirements:

```csharp
// Good starting point
kafka.Producer.Acks = Acks.All;
kafka.Producer.EnableIdempotence = true;
kafka.Consumer.EnableAutoCommit = false;
kafka.Consumer.IsolationLevel = IsolationLevel.ReadCommitted;
```

### 2. Environment-Specific Configuration

Use different configurations for different environments:

```csharp
var environment = builder.Environment.EnvironmentName;
kafka.ClientId = $"my-app-{environment}";
kafka.Consumer.GroupId = $"my-group-{environment}";

if (environment == "Production")
{
    kafka.LogLevel = 3; // Error level
    kafka.Producer.Acks = Acks.All;
}
else
{
    kafka.LogLevel = 6; // Info level
    kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
}
```

### 3. Security First

Always use security in production:

```csharp
if (builder.Environment.IsProduction())
{
    kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
    kafka.SaslMechanism = SaslMechanism.ScramSha256;
    kafka.SaslUsername = GetSecretValue("kafka-username");
    kafka.SaslPassword = GetSecretValue("kafka-password");
}
```

### 4. Monitor Performance

Enable monitoring to understand your system's behavior:

```csharp
kafka.StatisticsIntervalMs = 60000; // Statistics every minute
kafka.EnableDeliveryReports = true; // Track delivery success
```

## Next Steps


