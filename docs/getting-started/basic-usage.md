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
            
            topic.HasConsumer(consumer =>
            {
                consumer.HasExclusiveConnection(conn => 
                {
                    conn.GroupId = "order-processor";
                });
            });
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
        
        topic.HasConsumer(consumer =>           // Consumer configuration
        {
            consumer.HasExclusiveConnection(connection =>
            {
                connection.GroupId = "order-processor";
            });
            consumer.HasInbox(); // Enable inbox for deduplication
        });
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

### Publishing with the Outbox Pattern

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

        // Publish event (stored in outbox for reliable delivery)
        _dbContext.OrderEvents.Publish(new OrderCreated
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
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        // Global consumer settings
        client.Consumer.GroupId = "my-global-group";
    }));

// Register the outbox worker if you are using the outbox pattern
builder.Services.AddOutboxKafkaWorker<MyDbContext>();

// Register your consumer
builder.Services.AddHostedService<OrderEventProcessor>();

var app = builder.Build();
```