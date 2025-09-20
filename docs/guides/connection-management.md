# Connection Management

K-Entity-Framework uses a single, shared producer and consumer connection by default. This is the recommended approach based on community best practices.

### Why Single Connections?

### Producer Singleton Pattern
As discussed in [confluent-kafka-dotnet issue #1346](https://github.com/confluentinc/confluent-kafka-dotnet/issues/1346), using a producer instance as a singleton is the recommended approach for long-running applications. Creating and disposing producer instances repeatedly introduces significant overhead:

- **Connection establishment cost** - Each new producer instance must establish TCP connections and perform authentication
- **Memory allocation** - Frequent creation/disposal leads to unnecessary garbage collection pressure
- **Resource leaks** - Improper disposal can lead to connection exhaustion at the broker level
- **Performance impact** - Connection pooling and batching optimizations are lost with frequent recreation

### Consumer Instance Management
Based on [confluent-kafka-dotnet issue #197](https://github.com/confluentinc/confluent-kafka-dotnet/issues/197), when consuming from multiple topics or partitions within a single application, the recommended approach is:

- **Single consumer per consumer group** - Use one consumer instance that subscribes to multiple topics
- **Partition assignment** - Let Kafka handle partition assignment automatically through consumer group coordination
- **Avoid multiple consumer instances** - Multiple consumers in the same group for the same topics can lead to unnecessary rebalancing and reduced throughput

## Configuration

### Global Configuration

Configure shared connections for the entire application:

```csharp
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        client.Producer.BatchSize = 100000;
        client.Consumer.MaxBufferedMessages = 1000;
    }));
```

### Multiple Topics with Single Consumer

The shared consumer automatically handles multiple topics:

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("order-events");
    topic.HasConsumer(consumer => consumer.HasMaxBufferedMessages(500));
});

modelBuilder.Topic<PaymentProcessed>(topic =>
{
    topic.HasName("payment-events");
    topic.HasConsumer(consumer => consumer.HasMaxBufferedMessages(100));
});
```
> [!WARNING]
> Creating multiple consumer instances for the same consumer group and topics will cause continuous rebalancing, reduced throughput, and increased latency.

## When Multiple Connections Make Sense

Consider separate connections only for:

- **Different security contexts** - When topics require different authentication
- **Isolated failure domains** - When complete separation is required
- **Different consumer groups** - When consuming the same topics for different purposes

> Note: While shared connections are recommended for most scenarios, there are specific cases where separate connections are necessary or beneficial. Avoid creating more connections than necessary to limit resource usage and complexity.