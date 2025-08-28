# K-Entity-Framework Documentation

K-Entity-Framework is a lightweight .NET library for working with Apache Kafka. It provides a simple and efficient way to produce and consume messages in Kafka topics with a focus on Entity Framework integration and middleware-based extensibility.

## 📚 Table of Contents

### Getting Started
- [Quick Start Guide](docs/getting-started/quick-start.md) - Get up and running in minutes
- [Installation](docs/getting-started/installation.md) - Installation and setup instructions
- [Basic Usage](docs/getting-started/basic-usage.md) - Producer and consumer fundamentals

### Architecture
- [Middleware Architecture](docs/architecture/middleware-architecture.md) - Core middleware system design
- [Plugin System](docs/architecture/plugin-serialization.md) - Extensible plugin architecture
- [Service Attributes](docs/architecture/service-attributes.md) - Service lifetime management

### Features
- [Serialization](docs/features/serialization.md) - Multiple serialization framework support
- [Deduplication](docs/features/deduplication.md) - Message deduplication strategies
- [Type-Specific Processing](docs/features/type-specific-processing.md) - Per-type consumer configuration
- [Outbox Pattern](docs/features/outbox.md) - Transactional outbox implementation
- [Inbox Pattern](docs/features/inbox.md) - Message deduplication and idempotency
- [Connection Management](docs/features/connection-management.md) - Dedicated connection strategies

### Configuration
- [Kafka Configuration](docs/guides/kafka-configuration.md) - Complete configuration reference
- [Consumer Options](docs/guides/consumer-options.md) - Consumer-specific settings
- [Producer Options](docs/guides/producer-options.md) - Producer-specific settings
- [Middleware Configuration](docs/guides/middleware-configuration.md) - Middleware setup and options

### Examples
- [Basic Examples](docs/examples/basic-examples.md) - Simple producer and consumer examples
- [Advanced Examples](docs/examples/advanced-examples.md) - Complex scenarios and use cases
- [Integration Examples](docs/examples/integration-examples.md) - Real-world integration patterns

### Guides
- [Performance Tuning](docs/guides/performance-tuning.md) - Optimization strategies
- [Error Handling](docs/guides/error-handling.md) - Error handling patterns
- [Testing](docs/guides/testing.md) - Testing strategies and patterns
- [Deployment](docs/guides/deployment.md) - Production deployment considerations

## 🚀 Quick Example

### Setup DbContext with Kafka Integration
```csharp
public class MyDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public Topic<OrderCreated> OrderEvents { get; set; }

    public MyDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-created-topic");
            
            topic.UseSystemTextJson(settings =>
            {
                settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            
            topic.HasProducer(producer =>
            {
                producer.HasKey(o => o.OrderId);
                producer.HasOutbox(outbox =>
                {
                    outbox.UseBackgroundOnly();
                });
            });
            
            topic.HasConsumer(consumer =>
            {
                consumer.HasExclusiveConnection(connection =>
                {
                    connection.GroupId = "order-created-dedicated";
                    connection.MaxPollIntervalMs = 300000;
                });
                
                consumer.HasMaxBufferedMessages(2000);
                consumer.HasBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                
                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(o => new { o.OrderId, o.Status });
                    inbox.UseDeduplicationTimeWindow(TimeSpan.FromHours(1));
                });
            });
        });
    }
}

public class Order
{
    public int Id { get; set; }
    public string Status { get; set; }
}

public class OrderCreated
{
    public int OrderId { get; set; }
    public string Status { get; set; }
}
```

### Service Configuration
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Initial Catalog=Hello World")
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        client.Consumer.MaxBufferedMessages = 1000;
        client.Consumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
    }))
    .AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
        .WithMaxMessagesPerPoll(100)
        .WithPollingInterval(4000)
        .UseSingleNode());

var app = builder.Build();
```

### Producer
```csharp
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

// Add entity to database
dbContext.Orders.Add(new Order { Status = "New" });

// Publish event (stored in outbox for reliable delivery)
dbContext.OrderEvents.Publish(new OrderCreated { OrderId = 1, Status = Guid.NewGuid().ToString() });

// Save both entity and outbox message in same transaction
await dbContext.SaveChangesAsync();
```

### Consumer
```csharp
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

// Consume messages using async enumeration
await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(app.Lifetime.ApplicationStopping))
{
    Console.WriteLine($"Processing order: {orderEvent.OrderId} with status: {orderEvent.Status}");
    
    // Commit the message by saving changes
    await dbContext.SaveChangesAsync();
}
```

## 🏗️ Key Features

- **Entity Framework Integration** - Seamless integration with EF Core
- **Middleware Pipeline** - Extensible middleware system for message processing
- **Multiple Serialization Formats** - Support for JSON, MessagePack, and custom serializers
- **Message Deduplication** - Built-in deduplication strategies
- **Transactional Outbox** - Reliable message publishing with database transactions
- **Type-Safe Configuration** - Strongly-typed configuration API
- **High Performance** - Optimized for high-throughput scenarios
- **Plugin Architecture** - Extensible through plugins and custom middleware

## 📦 Installation

```bash
dotnet add package K.EntityFrameworkCore
```

## 🤝 Contributing

Contributions are welcome! Please read our [contributing guidelines](../CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE.txt) file for details.
