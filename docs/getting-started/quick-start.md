# Quick Start Guide

Get up and running with K-Entity-Framework in just a few minutes.

## Prerequisites

- .NET 8.0 or later
- Apache Kafka cluster (local or cloud)
- Entity Framework Core knowledge (recommended)

## Installation

Add the K-Entity-Framework package to your project:

```bash
dotnet add package K.EntityFrameworkCore
```

## Hello World Example

Here's a complete working example that demonstrates the core functionality:

```csharp
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddDbContext<OrderContext>(optionsBuilder => optionsBuilder
            .UseInMemoryDatabase("hello-world-db")
            .UseKafkaExtensibility(client => client.BootstrapServers = "localhost:9092"));

        using var app = builder.Build();
        app.Start();

        var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

    // Produce an event. It will be sent when SaveChangesAsync is called.
    dbContext.OrderEvents.Produce(new OrderEvent { Id = 1, Status = Guid.NewGuid().ToString() });

        await dbContext.SaveChangesAsync();

        // Consume events
        await foreach (var order in dbContext.OrderEvents)
        {
            Console.WriteLine($"Received order event: {order.Id} with status {order.Status}");
            // The message offset is automatically committed when SaveChangesAsync() is called.
            await dbContext.SaveChangesAsync();
            break; // Exit after processing one message for this example
        }

        await app.StopAsync();
    }
}

public class OrderContext(DbContextOptions options) : DbContext(options)
{
    public Topic<OrderEvent> OrderEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<OrderEvent>(topic =>
        {
            topic.HasName("hello-world-topic");
            topic.HasProducer(p => p.HasKey(m => m.Id));
            topic.HasConsumer(c => c.HasExclusiveConnection(conn => conn.GroupId = "hello-world-group"));
        });
    }
}

public class OrderEvent
{
    public int Id { get; set; }
    public string Status { get; set; }
}
```

### What's Happening?

1.  **Setup**: Configure your `DbContext` with Kafka integration using `UseKafkaExtensibility`.
2.  **Topic Property**: Add a `Topic<T>` property to your `DbContext` for each message type.
3.  **Topic Configuration**: Define your topics, producers, and consumers in `OnModelCreating` using the `modelBuilder.Topic<T>()` fluent API.
4.  **Producing**: Call `Produce()` on your `Topic<T>` property to queue an event for producing. The event is sent when `SaveChangesAsync()` is called.
5.  **Consuming**: Use `await foreach` on your `Topic<T>` property to consume events. Offsets are committed automatically on the next `SaveChangesAsync()` call.

## Next Steps

- [Basic Usage](basic-usage.md) - Learn the fundamentals in more detail.
- [Features](../features/index.md) - Explore advanced features like outbox, inbox, and serialization.