# Outbox Pattern

K-Entity-Framework provides a robust implementation of the Transactional Outbox pattern, ensuring reliable message delivery by storing messages in the same database transaction as your business data.

## Overview

The Outbox pattern solves the dual-write problem in distributed systems by:

- **Atomic Operations** - Messages and business data are stored in the same database transaction.
- **Guaranteed Delivery** - A background worker ensures all messages are eventually published to Kafka.

## Architecture

### Core Components

1.  **`OutboxMessage`** - An entity that stores pending messages in your database.
2.  **`OutboxProducerMiddleware<T>`** - Intercepts publish operations and stores messages in the outbox.
3.  **`OutboxPollingWorker<TDbContext>`** - A background service that polls the outbox and publishes messages.

### Message Flow

```mermaid
graph TD
    A[Application Code] --> B[dbContext.Topic.Publish()]
    B --> C[Outbox Middleware]
    C --> D[Store in OutboxMessage Table]
    D --> E[SaveChangesAsync]
    E --> F[Transaction Commit]
    F --> G[OutboxPollingWorker]
    G --> H[Publish to Kafka]
    
    style C fill:#fff3e0
    style G fill:#e1f5fe
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

Publishing a message with the outbox pattern is the same as regular publishing. The outbox middleware handles the rest.

```csharp
// Start a database transaction
using var transaction = await dbContext.Database.BeginTransactionAsync();

try
{
    // 1. Modify business data
    var order = new Order { CustomerId = "C123", Status = "Created" };
    dbContext.Set<Order>().Add(order);
    
    // 2. Publish an event (it will be stored in the outbox)
    dbContext.OrderEvents.Publish(new OrderCreated 
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