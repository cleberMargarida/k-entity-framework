# Outbox Pattern

K-Entity-Framework provides a robust implementation of the Transactional Outbox pattern, ensuring reliable message delivery by storing messages in the same database transaction as your business data.

## Overview

The Outbox pattern solves the dual-write problem in distributed systems by:

- **Atomic Operations** - Messages and business data are stored in the same database transaction
- **Guaranteed Delivery** - Background workers ensure all messages are eventually published to Kafka
- **Exactly-Once Semantics** - Prevents duplicate message publishing
- **High Performance** - Optimized polling and batching for high-throughput scenarios

## Architecture

### Core Components

1. **`OutboxMessage`** - Entity that stores pending messages in the database
2. **`OutboxMiddleware<T>`** - Intercepts publish operations and stores messages in outbox
3. **`OutboxPollingWorker<TDbContext>`** - Background service that publishes stored messages
4. **`OutboxProducerMiddleware<T>`** - Handles actual Kafka publishing from outbox

### Message Flow

```mermaid
graph TD
    A[Application Code] --> B[dbContext.Topic.Publish()]
    B --> C[OutboxMiddleware]
    C --> D[Store in OutboxMessage Table]
    D --> E[SaveChangesAsync]
    E --> F[Transaction Commit]
    F --> G[OutboxPollingWorker]
    G --> H[OutboxProducerMiddleware]
    H --> I[Kafka Topic]
    
    style C fill:#fff3e0
    style G fill:#e1f5fe
    style H fill:#c8e6c9
```

## Configuration

### Basic Outbox Setup

Configure outbox for a message type:

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("order-events");
    
    topic.HasProducer(producer =>
    {
        producer.HasKey(order => order.OrderId);
        producer.HasOutbox();
    });
});
```

### Outbox Publishing Strategies

Control when messages are published:

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasProducer(producer =>
    {
        producer.HasOutbox(outbox =>
        {
            // Only use background worker (default)
            outbox.UseBackgroundOnly();
            
            // Try immediate publish, fallback to background
            // outbox.UseImmediateWithFallback();
        });
    });
});
```

### Outbox Worker Configuration

Configure the background worker that processes outbox messages:

```csharp
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseKafkaExtensibility(kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
    }))
    .AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
        .WithMaxMessagesPerPoll(100)           // Batch size for efficiency
        .WithPollingInterval(TimeSpan.FromSeconds(5))  // How often to check for messages
        .UseSingleNode());                     // Coordination strategy
```

## Coordination Strategies

### Single Node Strategy

For single-instance deployments:

```csharp
.AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
    .UseSingleNode());
```

### Exclusive Node Strategy  

For clustered deployments where only one instance should process outbox:

```csharp
.AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
    .UseExclusiveNode());
```

### Work Balance Strategy (Future)

For distributed processing across multiple instances:

```csharp
.AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
    .WorkBalance());
```

## Usage Examples

### Transactional Message Publishing

```csharp
using var scope = serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

// Start database transaction
using var transaction = await dbContext.Database.BeginTransactionAsync();

try
{
    // Modify business data
    var order = new Order { CustomerId = "C123", Status = "Created" };
    dbContext.Orders.Add(order);
    
    // Publish event (stored in outbox within same transaction)
    dbContext.OrderEvents.Publish(new OrderCreated 
    { 
        OrderId = order.Id, 
        CustomerId = order.CustomerId 
    });
    
    // Commit both business data and outbox message atomically
    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Bulk Operations

```csharp
var orders = new List<Order>();
var events = new List<OrderCreated>();

for (int i = 0; i < 1000; i++)
{
    var order = new Order { CustomerId = $"C{i}", Status = "Created" };
    orders.Add(order);
    
    events.Add(new OrderCreated { OrderId = order.Id, CustomerId = order.CustomerId });
}

// Add all entities
dbContext.Orders.AddRange(orders);

// Publish all events (stored in outbox)
foreach (var orderEvent in events)
{
    dbContext.OrderEvents.Publish(orderEvent);
}

// Single transaction for all operations
await dbContext.SaveChangesAsync();
```

## Performance Considerations

### Polling Optimization

```csharp
.AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
    .WithMaxMessagesPerPoll(500)           // Larger batches for high volume
    .WithPollingInterval(TimeSpan.FromSeconds(1))); // More frequent polling
```

### Database Indexes

Ensure proper indexing on the OutboxMessage table:

```sql
-- Index for polling worker queries
CREATE INDEX IX_OutboxMessage_ProcessedAt_CreatedAt 
ON OutboxMessage (ProcessedAt, CreatedAt) 
WHERE ProcessedAt IS NULL;

-- Index for coordination strategies
CREATE INDEX IX_OutboxMessage_BucketId 
ON OutboxMessage (BucketId) 
WHERE ProcessedAt IS NULL;
```

### Cleanup Strategy

Old outbox messages should be cleaned up periodically:

```csharp
// Clean up processed messages older than 7 days
var cutoffDate = DateTime.UtcNow.AddDays(-7);
var processedMessages = dbContext.Set<OutboxMessage>()
    .Where(m => m.ProcessedAt.HasValue && m.ProcessedAt < cutoffDate);

dbContext.RemoveRange(processedMessages);
await dbContext.SaveChangesAsync();
```

## Advanced Configuration

### Custom Message Keys

```csharp
topic.HasProducer(producer =>
{
    producer.HasKey(order => $"{order.CustomerId}:{order.OrderId}");
    producer.HasOutbox();
});
```

### Error Handling

```csharp
.AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
    .WithMaxMessagesPerPoll(100)
    .WithPollingInterval(TimeSpan.FromSeconds(5))
    .WithRetryPolicy(retries => retries
        .MaxAttempts(3)
        .ExponentialBackoff(TimeSpan.FromSeconds(1))));
```

## Monitoring and Observability

### Metrics to Track

- **Outbox Message Count** - Number of pending messages
- **Processing Rate** - Messages processed per second
- **Processing Latency** - Time from message creation to Kafka publish
- **Error Rate** - Failed publish attempts

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<MyDbContext>()
    .AddKafka(options => 
    {
        options.BootstrapServers = "localhost:9092";
    })
    .AddOutboxHealthCheck<MyDbContext>(options =>
    {
        options.MaxPendingMessages = 10000;
        options.MaxMessageAge = TimeSpan.FromMinutes(5);
    });
```

## Best Practices

1. **Use Appropriate Batch Sizes** - Balance throughput vs. latency
2. **Monitor Outbox Table Size** - Implement cleanup strategies
3. **Choose Right Coordination Strategy** - Based on deployment model
4. **Index Database Properly** - For optimal polling performance
5. **Handle Poison Messages** - Implement dead letter queues for failed messages
6. **Monitor Worker Health** - Ensure background workers are running
7. **Test Transaction Boundaries** - Verify atomicity in failure scenarios
