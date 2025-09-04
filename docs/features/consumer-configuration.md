# Consumer Configuration

K-Entity-Framework allows you to configure consumer settings at a global level and also per message type. This enables fine-tuned performance optimization based on the characteristics and requirements of each message type in your system.

## Global Connections

By default, K-Entity-Framework registers a single, shared producer connection and a single, shared consumer connection for the entire application. This is a best practice that avoids resource exhaustion and is recommended by the Confluent Kafka client team.

### Global Consumer Configuration

You can configure global consumer settings that all message types will inherit. These settings are applied to the shared consumer connection.

```csharp
builder.Services.AddDbContext<MyDbContext>(optionsBuilder => optionsBuilder
    .UseSqlServer("...")
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        
        // Global defaults for all consumers
        client.Consumer.MaxBufferedMessages = 1000;
        client.Consumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
    }));
```

## Type-Specific Configuration

You can override the global settings for specific message types.

### Exclusive Connections

While there is a single shared consumer by default, you can configure a message type to use its own dedicated consumer connection. This is useful for high-priority messages or for isolating consumers from each other.

When you configure an exclusive connection, a new `KafkaConsumerPollService` is created for that specific message type, with its own underlying `IConsumer` instance.

```csharp
modelBuilder.Topic<CriticalAlert>(topic =>
{
    topic.HasName("critical-alerts");
    
    topic.HasConsumer(consumer =>
    {
        consumer.HasExclusiveConnection(connection =>
        {
            connection.GroupId = "critical-alerts-processor";
            connection.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    });
});
```

### Buffer and Backpressure

You can configure the buffer size and backpressure mode for each message type, regardless of whether it's using the shared or an exclusive consumer.

```csharp
// High-volume events - large buffer for throughput
modelBuilder.Topic<UserClickEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasMaxBufferedMessages(5000); // Larger buffer
        consumer.HasBackpressureMode(ConsumerBackpressureMode.DropOldestMessage); // Can drop old events
    });
});

// Critical events - conservative settings
modelBuilder.Topic<PaymentProcessed>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasMaxBufferedMessages(100); // Small buffer
        consumer.HasBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure); // Never drop
    });
});
```