# Topic Provisioning

K-Entity-Framework provides automatic Kafka topic provisioning through the `TopicProvisioningHostedService<TDbContext>`, ensuring that all configured topics exist before your application starts processing messages.

## Overview

The Topic Provisioning feature automatically creates Kafka topics based on configurations defined in your `DbContext` using the `HasSetting` method. This eliminates the need for manual topic creation and ensures consistency between your code configuration and Kafka cluster state.

## Architecture

[`ServiceCollectionExtensions.AddTopicProvisioning<TDbContext>`](../api/K.EntityFrameworkCore.Extensions.ServiceCollectionExtensions.yml) - Extension method for registering the service.

### Provisioning Flow

```mermaid
%% Topic provisioning flow
graph LR
    A["Application Startup"] --> B["TopicProvisioningHostedService"]
    B --> C["Scan DbContext Model"]
    C --> D["Find TopicSpecifications"]
    D --> E["Connect to Kafka Admin API"]
    E --> F["Create Missing Topics"]
    F --> G["Log Results"]

    %% Base style for all nodes
    classDef default fill:#2b2b2b,stroke:#888,stroke-width:1px,color:#eee;

    %% Highlight service
    class B service;
    classDef service fill:#0d47a1,stroke:#29b6f6,color:#bbdefb;

    %% Highlight Kafka operations
    class E,G kafka;
    classDef kafka fill:#1b5e20,stroke:#66bb6a,color:#a5d6a7;

    %% Highlight scanning
    class C,D scan;
    classDef scan fill:#3e2723,stroke:#ff9800,color:#ffcc80;
```

## Configuration

### Basic Setup

Configure topics in your `DbContext` using the `HasSetting` method:

```csharp
public class MyDbContext : DbContext
{
    public Topic<OrderCreated> OrderEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.HasSetting(spec =>
            {
                spec.NumPartitions = 3;
                spec.ReplicationFactor = 2;
                spec.Configs = new Dictionary<string, string>
                {
                    ["cleanup.policy"] = "delete",
                    ["retention.ms"] = "604800000", // 7 days
                    ["compression.type"] = "snappy"
                };
            });
            topic.HasProducer(producer => producer.HasOutbox());
        });
    }
}
```

### Service Registration

Register the topic provisioning service in your DI container:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register DbContext with Kafka integration
builder.Services.AddDbContext<EcommerceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
           .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

// Register topic provisioning
builder.Services.AddTopicProvisioning<EcommerceDbContext>();

var app = builder.Build();
```

## Topic Configuration Options

The `HasSetting` method accepts a `TopicSpecification` from Confluent.Kafka.Admin, allowing you to configure:

### Partitions and Replication

```csharp
topic.HasSetting(spec =>
{
    spec.NumPartitions = 3;        // Number of partitions
    spec.ReplicationFactor = 2;    // Replication factor
});
```

### Topic-Level Configurations

```csharp
topic.HasSetting(spec =>
{
    spec.Configs = new Dictionary<string, string>
    {
        ["cleanup.policy"] = "delete",           // delete or compact
        ["retention.ms"] = "604800000",          // 7 days in milliseconds
        ["compression.type"] = "snappy",         // compression codec
        ["min.cleanable.dirty.ratio"] = "0.1",   // for compact policy
        ["segment.ms"] = "86400000"              // segment time
    };
});
```

## Behavior and Lifecycle

### Startup Process

1. **Model Scanning** - The service scans your `DbContext` model for all `TopicSpecification` configurations.
2. **Kafka Connection** - Connects to Kafka using the Admin API (default: `localhost:9092`).
3. **Topic Discovery** - Checks which topics already exist in the Kafka cluster.
4. **Selective Creation** - Creates only topics that don't exist, skipping existing ones.
5. **Logging** - Provides comprehensive logging for monitoring and debugging.

### Idempotent Operations

The service is designed to be safe to run multiple times:

- **Existing Topics** - Topics that already exist are skipped without error.
- **Configuration Changes** - Only new topics are created; existing topic configurations are not modified.
- **Error Handling** - Individual topic creation failures don't prevent other topics from being created.

### Error Handling

- **Connection Failures** - If Kafka is unreachable, the application startup will fail.
- **Topic Creation Errors** - Real errors (not "already exists") will cause startup to fail.
- **Partial Failures** - If some topics fail to create, the service logs details and fails the startup.

### Prerequisites

- **Kafka Accessibility** - Kafka cluster must be accessible during application startup.
- **Permissions** - The application must have permissions to create topics in Kafka.
- **Network** - Ensure network connectivity between your application and Kafka brokers.

## Examples

### Complete Configuration

```csharp
public class EcommerceDbContext : DbContext
{
    public Topic<OrderCreated> OrderEvents { get; set; }
    public Topic<UserRegistered> UserEvents { get; set; }
    public Topic<PaymentProcessed> PaymentEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // High-throughput order events
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.HasSetting(spec =>
            {
                spec.NumPartitions = 6;
                spec.ReplicationFactor = 3;
                spec.Configs = new Dictionary<string, string>
                {
                    ["cleanup.policy"] = "delete",
                    ["retention.ms"] = "2592000000", // 30 days
                    ["compression.type"] = "lz4"
                };
            });
        });

        // User events with compaction
        modelBuilder.Topic<UserRegistered>(topic =>
        {
            topic.HasName("user-events");
            topic.HasSetting(spec =>
            {
                spec.NumPartitions = 3;
                spec.ReplicationFactor = 2;
                spec.Configs = new Dictionary<string, string>
                {
                    ["cleanup.policy"] = "compact",
                    ["min.cleanable.dirty.ratio"] = "0.1",
                    ["delete.retention.ms"] = "86400000" // 1 day
                };
            });
        });

        // Payment events with short retention
        modelBuilder.Topic<PaymentProcessed>(topic =>
        {
            topic.HasName("payment-events");
            topic.HasSetting(spec =>
            {
                spec.NumPartitions = 3;
                spec.ReplicationFactor = 2;
                spec.Configs = new Dictionary<string, string>
                {
                    ["cleanup.policy"] = "delete",
                    ["retention.ms"] = "604800000", // 7 days
                    ["compression.type"] = "snappy"
                };
            });
        });
    }
}
```

### Service Registration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register DbContext with SQL Server and Kafka integration
builder.Services.AddDbContext<EcommerceDbContext>(options => options
    
    // Configure Entity Framework with SQL Server
    .UseSqlServer(builder.Configuration.GetConnectionString("Default"))
    
    // Configure Kafka client connections
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")))

    // Register topic provisioning hosted service
    .AddTopicProvisioning<EcommerceDbContext>();

var app = builder.Build();
```

## API Reference

- `TopicProvisioningHostedService<TDbContext>`
- [`ServiceCollectionExtensions.AddTopicProvisioning<TDbContext>`](../api/K.EntityFrameworkCore.Extensions.ServiceCollectionExtensions.yml)
- [`ModelBuilderExtensions.Topic<T>`](../api/K.EntityFrameworkCore.Extensions.ModelBuilderExtensions.yml)
- [`TopicTypeBuilder<T>.HasSetting`](../api/K.EntityFrameworkCore.Extensions.TopicTypeBuilder-1.yml)