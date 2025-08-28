# Features

K-Entity-Framework provides a comprehensive set of features for building robust, high-performance Kafka-based applications.

## Core Features

### [Serialization](serialization.md)
Multiple serialization framework support with pluggable architecture.

- **System.Text.Json** (default) - High performance, modern JSON
- **Newtonsoft.Json** - Legacy compatibility and advanced JSON features  
- **MessagePack** - Binary serialization for maximum performance
- **Custom Serializers** - Plugin architecture for any serialization format

```csharp
// System.Text.Json
topic.UseJsonSerializer(options => 
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// MessagePack for high performance
topic.UseMessagePack(options => 
    options.Compression = MessagePackCompression.Lz4BlockArray);

// Custom serializer
topic.UseSerializer("ProtoBuf", options => { /* configure */ });
```

### [Message Deduplication](deduplication.md)
Prevent duplicate message processing with high-performance deduplication.

- **xxHash64** algorithm for fast hash computation
- **Expression trees** for type-safe value extraction
- **Configurable time windows** for deduplication scope
- **Composite keys** for complex deduplication scenarios

```csharp
consumer.HasInbox(inbox =>
{
    inbox.DeduplicateBy(order => order.OrderId)
         .UseDeduplicationTimeWindow(TimeSpan.FromHours(24));
});
```

### [Type-Specific Processing](type-specific-processing.md)
Configure different processing options per message type.

- **Per-type buffer sizes** for optimal memory usage
- **Backpressure strategies** tailored to message importance
- **Concurrency limits** based on processing characteristics
- **Inherits global defaults** with selective overrides

```csharp
// High-volume events
modelBuilder.Topic<UserClickEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasProcessing(processing =>
        {
            processing.WithMaxBufferedMessages(5000);
            processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
        });
    });
});

// Critical events
modelBuilder.Topic<PaymentProcessed>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasProcessing(processing =>
        {
            processing.WithMaxBufferedMessages(100);
            processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
        });
    });
});
```

## Reliability Features

### [Outbox Pattern](outbox.md)
Ensure reliable message publishing with database transactions.

- **Transactional guarantees** - Messages and database changes in same transaction
- **Automatic retry** for failed message delivery
- **Duplicate detection** for exactly-once delivery
- **Background processing** with configurable intervals

```csharp
topic.HasProducer(producer =>
{
    producer.HasOutbox(outbox =>
    {
        outbox.WithProcessingInterval(TimeSpan.FromSeconds(5));
        outbox.WithMaxRetries(3);
    });
});
```

### [Inbox Pattern](inbox.md)
Guarantee message idempotency and prevent duplicate processing.

- **Automatic deduplication** based on message content
- **Configurable retention** for processed messages
- **High-performance lookups** using indexed hash values
- **Integration with Entity Framework** for seamless persistence

```csharp
topic.HasConsumer(consumer =>
{
    consumer.HasInbox(inbox =>
    {
        inbox.DeduplicateBy(payment => new { 
            payment.OrderId, 
            payment.Amount 
        });
    });
});
```

### [Connection Management](connection-management.md)
Advanced connection strategies for different scenarios.

- **Shared connections** for resource efficiency
- **Dedicated connections** for isolation
- **Connection pooling** for high-throughput scenarios
- **Health monitoring** and automatic reconnection

## Performance Features

### High Throughput Optimization

- **Batching support** for producer and consumer operations
- **Parallel processing** with configurable concurrency
- **Zero-allocation paths** in steady-state operation
- **Compiled expressions** for fast property access

### Memory Management

- **Configurable buffer sizes** per message type
- **Backpressure handling** to prevent memory exhaustion
- **Efficient message serialization** with minimal allocations
- **Automatic cleanup** of processed messages

### Monitoring and Metrics

- **Built-in metrics** for throughput and latency
- **Health checks** for Kafka connectivity
- **Performance counters** for debugging
- **Structured logging** with correlation IDs

## Developer Experience

### Type-Safe Configuration

- **Strongly-typed APIs** with compile-time validation
- **Fluent configuration** for easy setup
- **IntelliSense support** for all configuration options
- **Clear error messages** for configuration issues

### Testing Support

- **Test utilities** for unit and integration testing
- **Mock-friendly design** for isolated testing
- **In-memory testing** support
- **Test containers** integration for realistic testing

### Debugging and Diagnostics

- **Detailed logging** with configurable levels
- **Performance profiling** hooks
- **Message tracking** across the pipeline
- **Configuration validation** at startup

## Integration Features

### Entity Framework Integration

- **Seamless DbContext integration** with shared transactions
- **Migration support** for inbox/outbox tables
- **Multi-database support** (SQL Server, PostgreSQL, MySQL, etc.)
- **Change tracking** integration for automatic event publishing

### Middleware Pipeline

- **Extensible architecture** for custom middleware
- **Composable pipelines** with configurable ordering
- **Built-in middleware** for common scenarios
- **Performance-optimized execution** with minimal overhead

### Cloud Platform Support

- **Confluent Cloud** with SASL authentication
- **Amazon MSK** with IAM integration
- **Azure Event Hubs** compatibility
- **Kubernetes** deployment patterns

## Advanced Features

### Plugin Architecture

- **Custom serializers** via plugin system
- **External middleware** integration
- **Runtime plugin discovery** and loading
- **Version compatibility** management

### Security

- **SASL/SSL** authentication support
- **Certificate-based** authentication
- **OAuth/OIDC** integration
- **Role-based access control** patterns

### Observability

- **OpenTelemetry** integration
- **Distributed tracing** support
- **Custom metrics** collection
- **Application insights** integration

## Coming Soon

### Planned Features

- **Schema Registry** integration
- **Avro serialization** support
- **Dead letter queues** with automatic retry
- **Message routing** and filtering
- **Stream processing** capabilities

### Experimental Features

- **WASM serializers** for maximum performance
- **GraphQL subscriptions** over Kafka
- **Event sourcing** patterns
- **CQRS** integration

## Feature Comparison

| Feature | Basic | Standard | Advanced |
|---------|-------|----------|----------|
| JSON Serialization | ✅ | ✅ | ✅ |
| Custom Serializers | ❌ | ✅ | ✅ |
| Message Deduplication | ❌ | ✅ | ✅ |
| Outbox Pattern | ❌ | ✅ | ✅ |
| Type-Specific Processing | ❌ | ❌ | ✅ |
| Advanced Monitoring | ❌ | ❌ | ✅ |
| Plugin Architecture | ❌ | ❌ | ✅ |

## Next Steps

- **[Getting Started](../getting-started/quick-start.md)** - Start using K-Entity-Framework
- **[Architecture](../architecture/middleware-architecture.md)** - Understand the system design  
- **[Examples](../examples/basic-examples.md)** - See practical implementations
- **[Guides](../guides/kafka-configuration.md)** - Learn configuration and best practices
