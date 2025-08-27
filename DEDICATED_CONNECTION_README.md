# Dedicated Consumer Connections

The Kafka Entity Framework Core extension now supports **Dedicated Consumer Connections**, allowing you to create separate, type-specific consumer instances with their own configurations. This feature provides isolation, scalability, and fine-grained control over how different message types are consumed.

## Overview

By default, all message types share a single `KafkaConsumerPollService` instance. With dedicated connections, you can create separate consumer poll services for specific message types, each with their own:

- Consumer configuration (group ID, timeouts, security settings, etc.)
- Processing settings (buffer sizes, backpressure modes)
- Consumer instances and connections
- Polling behavior and lifecycle

## Key Benefits

1. **Isolation**: Critical message types can have dedicated resources
2. **Scalability**: High-volume types can have optimized configurations  
3. **Security**: Different types can use different security configurations
4. **Performance**: Type-specific tuning for optimal throughput and latency
5. **Reliability**: Failures in one consumer don't affect others

## API Compatibility

The dedicated connection API mirrors the core registration API exactly:

```csharp
// Core registration (Program.cs)
client.Consumer.GroupId = "my-group";
client.Consumer.MaxPollIntervalMs = 300000;

// Dedicated connection (OnModelCreating)
consumer.HasDedicatedConnection(dedicatedConsumer =>
{
    dedicatedConsumer.GroupId = "my-dedicated-group";
    dedicatedConsumer.MaxPollIntervalMs = 300000;
});
```

## Usage Examples

### Basic Dedicated Connection

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasDedicatedConnection(dedicated =>
        {
            dedicated.WithConsumerGroupId("order-processing-dedicated");
            dedicated.WithStartImmediately(true);
        });
    });
});
```

### Advanced Configuration (Same API as Core Registration)

```csharp
modelBuilder.Topic<HighVolumeEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasDedicatedConnection(dedicatedConsumer =>
        {
            // Exactly the same API as client.Consumer
            dedicatedConsumer.GroupId = "high-volume-dedicated";
            dedicatedConsumer.MaxPollIntervalMs = 300000;
            dedicatedConsumer.SessionTimeoutMs = 45000;
            dedicatedConsumer.AutoOffsetReset = AutoOffsetReset.Latest;
            dedicatedConsumer.MaxBufferedMessages = 50000;
            dedicatedConsumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
        });
    });
});
```

### Multiple Dedicated Connections

```csharp
// Critical events - fast, small buffer
modelBuilder.Topic<CriticalAlert>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasDedicatedConnection(dedicatedConsumer =>
        {
            dedicatedConsumer.GroupId = "critical-alerts";
            dedicatedConsumer.MaxPollIntervalMs = 10000;
            dedicatedConsumer.MaxBufferedMessages = 100;
        });
    });
});

// Bulk events - slow, large buffer  
modelBuilder.Topic<BulkDataEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasDedicatedConnection(dedicatedConsumer =>
        {
            dedicatedConsumer.GroupId = "bulk-processor";
            dedicatedConsumer.MaxPollIntervalMs = 300000;
            dedicatedConsumer.MaxBufferedMessages = 10000;
            dedicatedConsumer.FetchMaxBytes = 52428800; // 50MB
        });
    });
});

// Regular events - shared consumer (no dedicated connection)
modelBuilder.Topic<RegularEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.WithMaxBufferedMessages(1000);
    });
});
```

### Security Configuration

```csharp
modelBuilder.Topic<SecureEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasDedicatedConnection(dedicatedConsumer =>
        {
            dedicatedConsumer.SecurityProtocol = SecurityProtocol.SaslSsl;
            dedicatedConsumer.SaslMechanism = SaslMechanism.Plain;
            dedicatedConsumer.SaslUsername = "secure-user";
            dedicatedConsumer.SaslPassword = "secure-password";
            dedicatedConsumer.SslCaLocation = "/path/to/ca-cert";
        });
    });
});
```

## Configuration Options

### Builder Methods

- `WithConsumerGroupId(string)`: Set dedicated consumer group ID
- `WithStartImmediately(bool)`: Control when the service starts (default: true)

### Consumer Configuration

All `IConsumerConfig` properties are available, including:

**Connection Settings:**
- `GroupId`: Consumer group ID
- `BootstrapServers`: Kafka broker addresses
- `ClientId`: Client identifier

**Polling Behavior:**
- `MaxPollIntervalMs`: Maximum time between polls
- `SessionTimeoutMs`: Session timeout
- `HeartbeatIntervalMs`: Heartbeat interval
- `AutoOffsetReset`: Offset reset strategy

**Performance Settings:**
- `FetchMaxBytes`: Maximum fetch size
- `MaxPartitionFetchBytes`: Per-partition fetch size
- `MaxBufferedMessages`: Buffer size (processing setting)
- `BackpressureMode`: Backpressure handling (processing setting)

**Security Settings:**
- `SecurityProtocol`: Security protocol
- `SaslMechanism`: SASL mechanism
- `SaslUsername`/`SaslPassword`: Credentials
- `SslCaLocation`: SSL CA certificate

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
├─────────────────────────────────────────────────────────────┤
│  Topic<OrderCreated>    │  Topic<BulkEvent>  │ Topic<Alert>  │
│  (dedicated)            │  (dedicated)       │ (shared)      │
├─────────────────────────────────────────────────────────────┤
│ KafkaConsumerPollService│KafkaConsumerPollService│           │
│ (OrderCreated)          │ (BulkEvent)           │ Shared    │
│                         │                       │ Service   │
├─────────────────────────────────────────────────────────────┤
│  IConsumer             │  IConsumer            │IConsumer   │
│  (dedicated instance)   │  (dedicated instance) │(shared)    │
├─────────────────────────────────────────────────────────────┤
│                    Kafka Cluster                            │
└─────────────────────────────────────────────────────────────┘
```

## Service Registration

The framework automatically:

1. Registers `DedicatedConsumerSettings<T>` for each message type
2. Registers `DedicatedKafkaConsumerPollServiceFactory` as singleton
3. Creates keyed services for type-specific `KafkaConsumerPollService` instances
4. Routes messages to the appropriate consumer based on configuration

## Performance Considerations

### When to Use Dedicated Connections

✅ **Good candidates:**
- High-volume message types requiring different buffer sizes
- Critical messages needing guaranteed resources
- Messages with different security requirements
- Types with vastly different processing characteristics

❌ **Avoid for:**
- Low-volume message types
- Types with similar processing needs
- When resource consumption is a concern

### Resource Impact

Each dedicated connection creates:
- A separate consumer instance
- A separate polling thread
- Additional memory for buffers
- Additional network connections

Monitor resource usage and connection limits in your Kafka cluster.

## Migration Guide

### From Shared to Dedicated

```csharp
// Before (shared consumer)
modelBuilder.Topic<MyEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.WithMaxBufferedMessages(5000);
    });
});

// After (dedicated consumer)
modelBuilder.Topic<MyEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasDedicatedConnection(dedicatedConsumer =>
        {
            dedicatedConsumer.GroupId = "my-event-dedicated";
            dedicatedConsumer.MaxBufferedMessages = 5000;
        });
    });
});
```

### Gradual Migration

You can migrate message types incrementally. Types without dedicated connections continue using the shared consumer, while types with dedicated connections get their own resources.

## Troubleshooting

### Common Issues

1. **Connection Limits**: Each dedicated connection uses a separate consumer instance
2. **Group Coordination**: Ensure unique group IDs for dedicated consumers
3. **Resource Usage**: Monitor memory and connection usage
4. **Startup Time**: Multiple dedicated connections may increase startup time

### Debugging

Enable Kafka client logging to monitor dedicated consumer behavior:

```csharp
// In your consumer configuration
dedicatedConsumer.Debug = "consumer,cgrp,topic,fetch";
```

## Best Practices

1. **Use meaningful group IDs**: Include message type in the group name
2. **Monitor resource usage**: Track memory, connections, and performance
3. **Start with shared consumers**: Only use dedicated when needed
4. **Test configuration changes**: Validate settings in development first
5. **Document dedicated connections**: Clearly document why each type needs dedicated resources

## Examples and Samples

See `DEDICATED_CONNECTION_USAGE_EXAMPLES.cs` for comprehensive examples of all usage patterns and configurations.
