# Basic Usage

Learn the fundamental concepts and usage patterns of K-Entity-Framework.

## Core Concepts

### 1. DbContext Integration

K-Entity-Framework extends Entity Framework Core's `DbContext` with Kafka capabilities. You add `Topic<T>` properties to your `DbContext` for each message type you want to produce or consume.

```csharp
public class MyDbContext : DbContext
{
    public Topic<OrderCreated> OrderEvents { get; set; }

    public MyDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure your topics
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.UseSystemTextJson();
            
            topic.HasProducer(producer =>
            {
                producer.HasKey(o => o.CustomerId);
                producer.HasOutbox();
            });
            
            topic.HasConsumer();
        });
    }
}
```

### 2. Topics

Topics represent Kafka topics and are configured in `OnModelCreating` using a fluent API. You can define the topic name, serialization, producer, and consumer behavior.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasName("order-events");          // Kafka topic name
        
        topic.UseSystemTextJson(); // Serialization
        
        topic.HasProducer(producer =>           // Producer configuration
        {
            producer.HasKey(o => o.CustomerId); // Partitioning key
            producer.HasOutbox();               // Transactional outbox
        });
        
        topic.HasConsumer();
    });
}
```

### 3. Message Types

Message types are simple C# classes (POCOs).

```csharp
public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
}
```

## Producer Usage

### Producing with the Outbox Pattern

```csharp
public class OrderService
{
    private readonly MyDbContext _dbContext;

    public OrderService(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order { /* ... */ };

        // Add to database
        _dbContext.Set<Order>().Add(order);

    // Produce event (stored in outbox for reliable delivery)
    _dbContext.OrderEvents.Produce(new OrderCreated
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = order.Amount,
        });

        // Save both entity and outbox message in the same transaction
        await _dbContext.SaveChangesAsync();
    }
}
```

## Consumer Usage

### Consuming with a Background Service

```csharp
public class OrderEventProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OrderEventProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
        {
            // Your business logic here
            Console.WriteLine($"Processing order {orderEvent.OrderId}");

            // This will commit the message offset and handle deduplication (if inbox is enabled)
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
```

## Service Registration

By default, K-Entity-Framework registers a single, shared producer connection and a single, shared consumer connection for the entire application. This is a best practice that avoids resource exhaustion. You can configure global settings for these connections.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register DbContext with Kafka integration
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

// Register the outbox worker if you are using the outbox pattern
builder.Services.AddOutboxKafkaWorker<MyDbContext>();

// Register your consumer
builder.Services.AddHostedService<OrderEventProcessor>();

var app = builder.Build();
```

## Examples

This section provides simple, practical examples to help you get started with K-Entity-Framework quickly. For short navigational convenience the examples that used to live in the "Basic Examples" page are consolidated here.

### Simple Producer and Consumer

#### DbContext Setup

```csharp
public class OrderDbContext : DbContext
{
    public Topic<OrderCreated> OrderEvents { get; set; }

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.HasProducer(producer => producer.HasKey(o => o.CustomerId));
            topic.HasConsumer(consumer => consumer.HasGroupId("order-processor"));
        });
    }
}

public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
}
```

#### Service Configuration (full)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(opts => opts
    // Configure Postgres
    .UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb"))
    
    // Configure Kafka
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

// If you use outbox patterns, add the worker
builder.Services.AddOutboxKafkaWorker<OrderDbContext>();

// Add background processor that reads from the DbContext topic
builder.Services.AddHostedService<OrderEventProcessor>();

var app = builder.Build();
app.Run();
```

#### Producer Example

```csharp
public class OrderService
{
    private readonly OrderDbContext _dbContext;

    public OrderService(OrderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateOrderAsync()
    {
    _dbContext.OrderEvents.Produce(new OrderCreated { OrderId = 1, CustomerId = "123" });
        await _dbContext.SaveChangesAsync();
    }
}
```

#### Consumer Example

```csharp
public class OrderEventProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OrderEventProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
        {
            Console.WriteLine($"Processing order {orderEvent.OrderId}");
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
```