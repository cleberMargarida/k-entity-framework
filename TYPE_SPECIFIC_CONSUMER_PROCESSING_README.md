# Type-Specific Consumer Processing Configuration

## Overview

This feature allows you to configure different `IConsumerProcessingConfig` settings per message type in your Kafka consumers. Previously, all message types shared the same global consumer processing configuration. Now each message type can have its own buffer size and backpressure behavior.

## Motivation

In Kafka consumers that handle multiple message types, different types often have different processing characteristics:

- **High-volume events** (like user clicks) might benefit from larger buffers for better throughput
- **Critical business events** (like payments) need conservative settings to prevent message loss
- **Bulk data imports** might tolerate message dropping for better performance
- **Real-time alerts** need small buffers for immediate processing

## Architecture

The implementation integrates consumer processing configuration directly into the existing `ConsumerMiddlewareSettings<T>`:

```
ConsumerMiddlewareSettings<T> : MiddlewareSettings<T>, IConsumerProcessingConfig
```

- **Type-specific settings**: Each message type `T` gets its own `ConsumerMiddlewareSettings<T>` instance with integrated processing config
- **Inherits global defaults**: Settings start with values from the global `client.Consumer.Processing` configuration
- **Selective overrides**: You can override specific settings per type while keeping others as defaults
- **Channel isolation**: Each message type maintains its own channel with its own processing configuration
- **Simplified architecture**: No separate settings classes - everything is integrated into the existing middleware pattern

## Usage

### 1. Global Configuration (Baseline)

Configure global defaults that all message types inherit:

```csharp
builder.Services.AddDbContext<MyDbContext>(optionsBuilder => optionsBuilder
    .UseSqlServer("...")
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        
        // Global defaults for all message types
        client.Consumer.Processing.MaxBufferedMessages = 1000;
        client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
    }));
```

### 2. Type-Specific Configuration

Override settings for specific message types:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // High-volume events - large buffer for throughput
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(5000); // Override buffer size
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
            });
            
            consumer.HasInbox(inbox => { ... });
        });
    });

    // Critical events - conservative settings
    modelBuilder.Topic<PaymentProcessed>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(500); // Smaller buffer
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure); // Never drop
            });
        });
    });

    // Bulk import - can tolerate drops for performance
    modelBuilder.Topic<BulkDataImport>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(10000);
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage); // Allow drops
            });
        });
    });

    // Standard events - use global defaults (no HasProcessing call needed)
    modelBuilder.Topic<StandardEvent>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasInbox(inbox => { ... });
            // No HasProcessing() - inherits global defaults
        });
    });
}
```

## API Reference

### ConsumerBuilder.HasProcessing()

```csharp
public ConsumerBuilder<T> HasProcessing(Action<ConsumerProcessingBuilder<T>>? configure = null)
```

Configures type-specific consumer processing settings for the message type `T`.

### ConsumerProcessingBuilder<T>

```csharp
public class ConsumerProcessingBuilder<T>
{
    // Set maximum buffered messages for this type
    public ConsumerProcessingBuilder<T> WithMaxBufferedMessages(int maxMessages);
    
    // Set backpressure behavior for this type
    public ConsumerProcessingBuilder<T> WithBackpressureMode(ConsumerBackpressureMode mode);
}
```

## Configuration Inheritance

1. **Global defaults**: Set via `client.Consumer.Processing.*`
2. **Type-specific overrides**: Set via `consumer.HasProcessing()`
3. **Fallback behavior**: If `HasProcessing()` is not called, the type uses global defaults
4. **Partial overrides**: You can override just `MaxBufferedMessages` or just `BackpressureMode`

## Migration Guide

### Before (Global Configuration Only)

```csharp
// All message types shared these settings
client.Consumer.Processing.MaxBufferedMessages = 1000;
client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
```

### After (Type-Specific Configuration)

```csharp
// Global defaults (baseline for all types)
client.Consumer.Processing.MaxBufferedMessages = 1000;
client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;

// Per-type overrides in OnModelCreating
modelBuilder.Topic<HighVolumeEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasProcessing(processing =>
        {
            processing.WithMaxBufferedMessages(5000); // Override for this type
        });
    });
});
```

## Best Practices

1. **Start with good global defaults** that work for most of your message types
2. **Override selectively** for types with special requirements
3. **High-volume types**: Use larger buffers for better throughput
4. **Critical types**: Use conservative settings to prevent message loss
5. **Analytics/logging types**: Consider allowing message drops for performance
6. **Real-time types**: Use smaller buffers for immediate processing

## Examples

See `TYPE_SPECIFIC_CONSUMER_PROCESSING_EXAMPLES.cs` for comprehensive examples covering:

- Different processing settings per message type
- Memory-constrained vs high-throughput scenarios  
- Real-time vs batch processing patterns
- Inheriting global defaults with selective overrides
