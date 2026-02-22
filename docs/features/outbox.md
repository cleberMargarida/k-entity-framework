# Outbox Pattern

K-Entity-Framework provides a robust implementation of the Transactional Outbox pattern, ensuring reliable message delivery by storing messages in the same database transaction as your business data.

## Overview

The Outbox pattern solves the dual-write problem in distributed systems by:

- **Atomic Operations** - Messages and business data are stored in the same database transaction.
- **Guaranteed Delivery** - A background worker ensures all messages are eventually produced to Kafka.

## Architecture

### Core Components

1.  [`OutboxMessage`](../api/K.EntityFrameworkCore.OutboxMessage.yml) - An entity that stores pending messages in your database.
2.  [`OutboxProducerMiddleware<T>`](../api/K.EntityFrameworkCore.Middlewares.Outbox.OutboxMiddlewareSettings-1.yml) - Intercepts produce operations and stores messages in the outbox.
3.  [`OutboxPollingWorker<TDbContext>`](../api/K.EntityFrameworkCore.Extensions.OutboxPollingWorkerSettings-1.yml) - A background service that polls the outbox and publishes messages.

### Message Flow

```mermaid
%% Dark mode styled flow
graph LR
    A["Application Code"] --> B["dbContext.Topic.Produce()"]
    B --> C["Outbox Middleware"]
    C --> D["Store in DbSet OutboxMessage"]
    D --> E["SaveChangesAsync"]
    E --> F["Transaction Commit"]
    F --> G["OutboxPollingWorker"]
    G --> H["Produce to Kafka"]

    %% Base style for all nodes
    classDef default fill:#2b2b2b,stroke:#888,stroke-width:1px,color:#eee;

    %% Highlight middleware
    class C middleware;
    classDef middleware fill:#3e2723,stroke:#ff9800,color:#ffcc80;

    %% Highlight worker
    class G worker;
    classDef worker fill:#0d47a1,stroke:#29b6f6,color:#bbdefb;

    %% Highlight database action
    class D database;
    classDef database fill:#263238,stroke:#4db6ac,color:#80cbc4;

    %% Highlight final Kafka step
    class H kafka;
    classDef kafka fill:#1b5e20,stroke:#66bb6a,color:#a5d6a7;
```

## Configuration

### 1. Enable Outbox on a Producer

In your `DbContext.OnModelCreating`, use the `HasOutbox()` method on a producer configuration.

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("order-events");
    
    topic.HasProducer(producer =>
    {
        producer.HasKey(order => order.OrderId);
        producer.HasOutbox(); // This enables the outbox for this message type
    });
});
```

### 2. Configure the Outbox Worker

In your `Program.cs` or `Startup.cs`, register the `OutboxPollingWorker`.

```csharp
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseKafkaExtensibility(kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
    }));

builder.Services.AddOutboxKafkaWorker<MyDbContext>(worker => worker
    .PollingInterval(TimeSpan.FromSeconds(5))
    .MaxMessagesPerPoll(100)
    .UseSingleNode() // or .UseExclusiveNode() for clustered deployments
);
```

## Usage

Producing a message with the outbox pattern is the same as regular producing. The outbox middleware handles the rest.

```csharp
// Start a database transaction
using var transaction = await dbContext.Database.BeginTransactionAsync();

try
{
    // 1. Modify business data
    var order = new Order { CustomerId = "C123", Status = "Created" };
    dbContext.Set<Order>().Add(order);
    
    // 2. Produce an event (it will be stored in the outbox)
    dbContext.OrderEvents.Produce(new OrderCreated 
    { 
        OrderId = order.Id, 
        CustomerId = order.CustomerId 
    });
    
    // 3. Commit both business data and the outbox message atomically
    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Examples

### Full service configuration

The examples below assume an ASP.NET Core application. Register the DbContext and Kafka client before configuring topics.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PostgreTestContext>(opts => opts
    
    // DbContext with PostgreSQL (replace connection string accordingly)
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    
    // Register DbContext and configure Kafka extensibility on it
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 50;
});

var app = builder.Build();
app.Run();
```

### Background-only outbox

Use this strategy when durability is more important than immediate delivery. Messages are stored in the outbox table and a worker publishes them asynchronously.

Configuration (topic-level):

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("outbox-test-topic");
    topic.HasProducer(producer =>
    {
        producer.HasKey(m => m.OrderId.ToString());
        producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
    });
});
```

Producer usage (application code):

```csharp
// Enqueue the message and persist the DbContext change. The message will
// be recorded in the outbox table and published by the background worker.
dbContext.OrderEvents.Produce(new OrderCreated { OrderId = 42, CustomerId = "OutboxTest" });
await dbContext.SaveChangesAsync();
```

### Immediate with fallback

This strategy first adds the message to the outbox for potential retries by the worker, then immediately attempts to publish it synchronously during `SaveChanges`. If publishing succeeds, the message is removed from the outbox, eliminating the need for worker processing.

Topic configuration:

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("immediate-fallback-topic");
    topic.HasProducer(p => p.HasKey(m => m.OrderId.ToString())
        .HasOutbox(o => o.UseImmediateWithFallback()));
});
```

Producer usage is identical from the app perspective — call SaveChanges and the library performs the immediate publish attempt with fallback.

### Batch publishing notes

When producing many messages in one DbContext SaveChanges operation, the library will handle batching according to the worker settings and producer configuration. Tune worker batch sizes and polling intervals in `AddOutboxKafkaWorker` options.

## Alternative: Debezium CDC Integration

For high-throughput scenarios requiring minimal latency, you can integrate the outbox table with Debezium and Kafka Connect for Change Data Capture (CDC). This approach:

- Eliminates polling overhead by capturing changes directly from transaction logs
- Provides near real-time event delivery (milliseconds vs seconds)
- Offloads event publishing to dedicated infrastructure
- Maintains strict ordering guarantees

**See the comprehensive guide**: [Debezium Integration](../guides/debezium-overview.md)

This guide includes:
- Complete Docker Compose setup for Debezium, Kafka Connect, and SQL Server
- Connector configuration for the outbox pattern
- CDC enablement steps
- Comparison between polling worker and Debezium approaches