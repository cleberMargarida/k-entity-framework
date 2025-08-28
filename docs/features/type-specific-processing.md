# Type-Specific Consumer Processing

K-Entity-Framework allows you to configure different consumer processing settings per message type. This enables fine-tuned performance optimization based on the characteristics and requirements of each message type in your system.

## Overview

Previously, all message types shared the same global consumer processing configuration. With type-specific processing configuration, each message type can have its own:

- **Buffer Size** - How many messages to buffer in memory
- **Backpressure Behavior** - How to handle buffer overflow situations
- **Processing Parallelism** - Concurrent processing limits
- **Error Handling** - Type-specific error handling strategies

## Motivation

In Kafka consumers that handle multiple message types, different types often have different processing characteristics:

- **High-volume events** (like user clicks) might benefit from larger buffers for better throughput
- **Critical business events** (like payments) need conservative settings to prevent message loss
- **Bulk data imports** might tolerate message dropping for better performance
- **Real-time alerts** need small buffers for immediate processing

## Architecture

The implementation integrates consumer processing configuration directly into the existing `ConsumerMiddlewareSettings<T>`:

```csharp
ConsumerMiddlewareSettings<T> : MiddlewareSettings<T>, IConsumerProcessingConfig
```

### Key Design Principles

- **Type-specific settings**: Each message type `T` gets its own `ConsumerMiddlewareSettings<T>` instance
- **Inherits global defaults**: Settings start with values from the global `client.Consumer.Processing` configuration
- **Selective overrides**: You can override specific settings per type while keeping others as defaults
- **Channel isolation**: Each message type maintains its own channel with its own processing configuration
- **Simplified architecture**: No separate settings classes - everything is integrated into the existing middleware pattern

## Configuration

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
        client.Consumer.Processing.MaxConcurrency = Environment.ProcessorCount;
    }));
```

### 2. Type-Specific Configuration

Override settings for specific message types based on their characteristics:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // High-volume events - large buffer for throughput
    modelBuilder.Topic<UserClickEvent>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(5000); // Larger buffer
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage); // Can drop old events
                processing.WithMaxConcurrency(8); // Higher parallelism
            });
        });
    });

    // Critical events - conservative settings
    modelBuilder.Topic<PaymentProcessed>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(100); // Small buffer
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure); // Never drop
                processing.WithMaxConcurrency(1); // Sequential processing
            });
        });
    });

    // Bulk import - optimized for throughput
    modelBuilder.Topic<BulkDataImport>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(10000); // Very large buffer
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage); // Allow drops
                processing.WithMaxConcurrency(Environment.ProcessorCount * 2); // High parallelism
            });
        });
    });

    // Real-time alerts - low latency
    modelBuilder.Topic<AlertEvent>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(10); // Minimal buffer
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure); // No drops
                processing.WithMaxConcurrency(1); // Immediate processing
            });
        });
    });

    // Standard events - use global defaults (no HasProcessing call needed)
    modelBuilder.Topic<StandardEvent>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasInbox(); // Uses global processing defaults
        });
    });
}
```

## Processing Configuration Options

### Buffer Size Configuration

Control how many messages are buffered in memory:

```csharp
consumer.HasProcessing(processing =>
{
    processing.WithMaxBufferedMessages(2000);
});
```

**Considerations:**
- **Larger buffers**: Better throughput, higher memory usage, increased latency
- **Smaller buffers**: Lower memory usage, reduced latency, potential throughput reduction
- **Memory impact**: Each buffered message consumes memory proportional to message size

### Backpressure Mode Configuration

Control what happens when the buffer is full:

```csharp
// Apply backpressure (default) - slow down consumption
consumer.HasProcessing(processing =>
{
    processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
});

// Drop oldest messages - maintain throughput at cost of message loss
consumer.HasProcessing(processing =>
{
    processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
});

// Drop newest messages - preserve older messages
consumer.HasProcessing(processing =>
{
    processing.WithBackpressureMode(ConsumerBackpressureMode.DropNewestMessage);
});
```

### Concurrency Configuration

Control how many messages are processed in parallel:

```csharp
consumer.HasProcessing(processing =>
{
    processing.WithMaxConcurrency(4); // Process up to 4 messages simultaneously
});
```

**Guidelines:**
- **CPU-bound processing**: Set to `Environment.ProcessorCount`
- **I/O-bound processing**: Can be higher than CPU count
- **Sequential processing**: Set to `1` for order-dependent operations
- **Resource constraints**: Consider database connection pools and external API limits

## Usage Scenarios

### E-commerce Platform

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Order events - critical, must not be lost
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(500);
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                processing.WithMaxConcurrency(2); // Limited concurrency for data consistency
            });
            
            consumer.HasInbox(inbox =>
            {
                inbox.DeduplicateBy(order => order.OrderId);
            });
        });
    });

    // Inventory updates - high volume, some loss acceptable
    modelBuilder.Topic<InventoryUpdated>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(2000);
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
                processing.WithMaxConcurrency(8); // High throughput
            });
        });
    });

    // User analytics - very high volume, loss acceptable
    modelBuilder.Topic<UserClickEvent>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(10000);
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
                processing.WithMaxConcurrency(16);
            });
        });
    });
}
```

### Financial Services

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Payment transactions - absolutely critical
    modelBuilder.Topic<PaymentTransaction>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(50); // Small buffer for quick processing
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                processing.WithMaxConcurrency(1); // Sequential processing for consistency
            });
            
            consumer.HasInbox(inbox =>
            {
                inbox.DeduplicateBy(payment => new { 
                    payment.TransactionId, 
                    payment.Amount 
                });
            });
        });
    });

    // Fraud detection alerts - time-sensitive
    modelBuilder.Topic<FraudAlert>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(20); // Minimal latency
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                processing.WithMaxConcurrency(4); // Parallel fraud analysis
            });
        });
    });

    // Market data - high volume, some loss acceptable
    modelBuilder.Topic<MarketDataEvent>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(5000);
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
                processing.WithMaxConcurrency(12);
            });
        });
    });
}
```

### IoT and Telemetry

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Device heartbeats - regular, high volume
    modelBuilder.Topic<DeviceHeartbeat>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(8000);
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
                processing.WithMaxConcurrency(16);
            });
        });
    });

    // Critical device alerts - must be processed
    modelBuilder.Topic<CriticalDeviceAlert>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(100);
                processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                processing.WithMaxConcurrency(4);
            });
        });
    });

    // Sensor data - very high volume, sampling acceptable
    modelBuilder.Topic<SensorReading>(topic =>
    {
        topic.HasConsumer(consumer =>
        {
            consumer.HasProcessing(processing =>
            {
                processing.WithMaxBufferedMessages(15000);
                processing.WithBackpressureMode(ConsumerBackpressureMode.DropNewestMessage); // Keep latest
                processing.WithMaxConcurrency(32);
            });
        });
    });
}
```

## Performance Tuning

### Buffer Size Optimization

```csharp
// For latency-sensitive applications
processing.WithMaxBufferedMessages(10); // Minimal buffering

// For throughput-optimized applications  
processing.WithMaxBufferedMessages(5000); // Large buffers

// For memory-constrained environments
processing.WithMaxBufferedMessages(100); // Conservative buffering
```

### Concurrency Optimization

```csharp
// CPU-bound processing
processing.WithMaxConcurrency(Environment.ProcessorCount);

// I/O-bound processing (database, API calls)
processing.WithMaxConcurrency(Environment.ProcessorCount * 2);

// Resource-constrained processing
processing.WithMaxConcurrency(Math.Max(1, Environment.ProcessorCount / 2));

// Sequential processing (order matters)
processing.WithMaxConcurrency(1);
```

### Backpressure Strategy Selection

```csharp
// For critical business data
processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);

// For real-time data where latest is most important
processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);

// For scenarios where historical data is important
processing.WithBackpressureMode(ConsumerBackpressureMode.DropNewestMessage);
```

## Monitoring and Metrics

### Buffer Utilization

Monitor buffer usage to optimize settings:

```csharp
public class ProcessingMetrics
{
    public void RecordBufferUtilization(string messageType, int currentSize, int maxSize)
    {
        var utilization = (double)currentSize / maxSize;
        
        _metrics.Gauge("consumer.buffer_utilization")
               .WithTag("message_type", messageType)
               .Set(utilization);
    }
    
    public void RecordMessageDropped(string messageType, string reason)
    {
        _metrics.Counter("consumer.messages_dropped")
               .WithTag("message_type", messageType)
               .WithTag("reason", reason)
               .Increment();
    }
}
```

### Processing Performance

Track processing performance per message type:

```csharp
public class ProcessingPerformanceTracker
{
    public void RecordProcessingTime(string messageType, TimeSpan duration)
    {
        _metrics.Timer("consumer.processing_time")
               .WithTag("message_type", messageType)
               .Record(duration);
    }
    
    public void RecordThroughput(string messageType, int messagesProcessed)
    {
        _metrics.Counter("consumer.messages_processed")
               .WithTag("message_type", messageType)
               .Increment(messagesProcessed);
    }
}
```

## Error Handling

### Type-Specific Error Handling

```csharp
public class TypeSpecificErrorHandler<T>
{
    public async Task<bool> HandleError(Exception ex, T message)
    {
        // Type-specific error handling logic
        return typeof(T).Name switch
        {
            nameof(PaymentTransaction) => await HandlePaymentError(ex, (PaymentTransaction)(object)message),
            nameof(UserClickEvent) => HandleClickEventError(ex, (UserClickEvent)(object)message),
            _ => await HandleGenericError(ex, message)
        };
    }
    
    private async Task<bool> HandlePaymentError(Exception ex, PaymentTransaction payment)
    {
        // Critical error - retry with backoff
        _logger.LogError(ex, "Payment processing failed for transaction {TransactionId}", 
            payment.TransactionId);
        
        // Send to dead letter queue for manual review
        await _deadLetterService.SendAsync(payment);
        return false; // Don't retry automatically
    }
    
    private bool HandleClickEventError(Exception ex, UserClickEvent clickEvent)
    {
        // Non-critical error - log and continue
        _logger.LogWarning(ex, "Click event processing failed for user {UserId}", 
            clickEvent.UserId);
        return true; // Skip and continue processing
    }
}
```

## Best Practices

### 1. Start with Global Defaults

- Configure sensible global defaults that work for most message types
- Only override settings for message types with specific requirements
- Regularly review and adjust global defaults based on monitoring

### 2. Match Settings to Business Requirements

- **Critical data**: Use conservative settings with backpressure
- **High-volume data**: Use larger buffers and allow message dropping
- **Real-time data**: Use small buffers for low latency
- **Ordered data**: Use sequential processing (concurrency = 1)

### 3. Monitor and Adjust

- Track buffer utilization to optimize buffer sizes
- Monitor message drop rates to understand system behavior
- Measure processing latency and throughput per message type
- Adjust settings based on production metrics

### 4. Consider Resource Constraints

- **Memory**: Larger buffers use more memory
- **CPU**: Higher concurrency may not always improve performance
- **Database**: Limit concurrency based on connection pool size
- **External APIs**: Respect rate limits when setting concurrency

### 5. Test Under Load

- Load test different configurations to find optimal settings
- Test backpressure scenarios to understand system behavior
- Verify error handling works correctly under high load
- Validate that critical messages are never lost

## Migration Guide

### From Global Configuration

If you're currently using global consumer configuration, you can gradually migrate to type-specific settings:

```csharp
// Before: Global configuration only
client.Consumer.Processing.MaxBufferedMessages = 1000;

// After: Global defaults + type-specific overrides
client.Consumer.Processing.MaxBufferedMessages = 1000; // Keep as default

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

### Configuration Migration Steps

1. **Identify message types** with different processing requirements
2. **Start with current global settings** as the baseline
3. **Add type-specific overrides** for message types that need different settings
4. **Monitor performance** and adjust settings based on metrics
5. **Gradually optimize** other message types as needed

## Next Steps

- [Performance Tuning](../guides/performance-tuning.md) - Learn more optimization strategies
- [Monitoring](../guides/monitoring.md) - Set up comprehensive monitoring
- [Examples](../examples/type-specific-examples.md) - See more configuration examples
