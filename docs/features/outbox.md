# Outbox Pattern

K-Entity-Framework provides a robust implementation of the Transactional Outbox pattern, ensuring reliable message delivery by storing messages in the same database transaction as your business data.

## Overview

The Outbox pattern solves the dual-write problem in distributed systems by:

- **Atomic Operations** - Messages and business data are stored in the same database transaction.
- **Guaranteed Delivery** - A background worker ensures all messages are eventually produced to Kafka.

## Architecture

### Core Components

1.  **`OutboxMessage`** - An entity that stores pending messages in your database.
2.  **`OutboxProducerMiddleware<T>`** - Intercepts produce operations and stores messages in the outbox.
3.  **`OutboxPollingWorker<TDbContext>`** - A background service that polls the outbox and publishes messages.

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