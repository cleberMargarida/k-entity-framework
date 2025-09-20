# Getting Started

`K.EntityFrameworkCore` is a library that seamlessly integrates Apache Kafka messaging with Entity Framework Core. It simplifies the process of producing and consuming messages in response to entity changes. 
<br/><br/>
For example, when you create an order, you may want to produce a message notifying other systems about the new order. 
<br/><br/>
The goal is to make this integration easy and smooth, allowing developers to focus on business logic rather than messaging infrastructure.

> Prerequisites
> - .NET 8.0 or later
> - Entity Framework Core
> - Kafka - messaging concepts and terminology

#### Define Your DbContext

Extend your Entity Framework Core DbContext to include Kafka topics alongside your regular entities:

```csharp
// Define your DbContext with Kafka topics
public class OrderContext(DbContextOptions options) : DbContext(options)
{
    // Standard EF Core DbSet for entities
    public DbSet<Order> Orders { get; set; }

    // Kafka topic for order events - enables producing and consuming messages
    public Topic<OrderEvent> OrderEvents { get; set; }
}
```

#### Configure Services

Set up dependency injection to configure your DbContext with both database and Kafka connectivity:

```csharp
// Configure services in your application startup
builder.Services.AddDbContext<MyDbContext>(options => options

  // Configure EF Core to use SQL Server
  .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Catalog=Hello World")

  // Enable Kafka extensibility for EF Core (publishing/consuming integration)
  .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));
```

#### Producing Messages

Create entities and produce Kafka messages that will be sent atomically when changes are saved:

```csharp
// Create and add a new order entity
dbContext.Orders.Add(new Order { Id = 1232 });

// Produce an order created event (non-blocking, queued for sending)
dbContext.OrderEvents.Produce(new OrderCreated { OrderId = 123 });

// Save changes - this persists the order AND sends the Kafka message atomically
await dbContext.SaveChangesAsync();
```

#### Consuming Messages

Process incoming messages from Kafka topics with automatic offset management:

```csharp
// Consume messages from the order events topic
// This starts consuming from Kafka and processes each message
await foreach (var order in dbContext.OrderEvents)
{
    // Process the consumed message here
    // ... your business logic ...

    // Commit the message offset to mark it as processed
    await dbContext.SaveChangesAsync(); // Commit message
}
```