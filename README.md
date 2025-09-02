# K-Entity-Framework Documentation

[![Build Status](https://github.com/cleberMargarida/k-entity-framework/actions/workflows/dotnet.yml/badge.svg)](https://github.com/cleberMargarida/k-entity-framework/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/K.EntityFrameworkCore.svg)](https://www.nuget.org/packages/K.EntityFrameworkCore/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/K.EntityFrameworkCore.svg)](https://www.nuget.org/packages/K.EntityFrameworkCore/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://cleberMargarida.github.io/k-entity-framework/)
[![GitHub Release](https://img.shields.io/github/v/release/cleberMargarida/k-entity-framework)](https://github.com/cleberMargarida/k-entity-framework/releases/latest)

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

## 🚀 Quick Start

### 1. Service Registration
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyApp;Trusted_Connection=true")
    .UseKafkaExtensibility(kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
    }))
    .AddOutboxKafkaWorker<MyDbContext>();

var app = builder.Build();
```

### 2. Define Your Message Types
```csharp
public class OrderCreated
{
    public int OrderId { get; set; }
    public string Status { get; set; }
}
```

### 3. Setup DbContext with Kafka Topics
```csharp
public class MyDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public Topic<OrderCreated> OrderEvents { get; set; }

    public MyDbContext(DbContextOptions options) : base(options) { }


public class Order
{
    public int Id { get; set; }
    public string Status { get; set; }
}
```

### 4. Producing Messages
```csharp
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

// Add entity to database
dbContext.Orders.Add(new Order { Status = "Created" });

// Publish event
dbContext.OrderEvents.Publish(new OrderCreated { OrderId = 123, Status = "Created" });

// Save entity and ensure publication
await dbContext.SaveChangesAsync();
```

### 5. Consuming Messages
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

That's it! You now have a working Kafka producer and consumer integrated with Entity Framework Core.

For advanced configuration options like serialization settings, outbox configuration, consumer settings, and deduplication strategies, see the detailed documentation sections below.

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
