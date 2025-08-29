# Basic Usage

Learn the fundamental concepts and usage patterns of K-Entity-Framework.

## Hello World Example

Here's a complete minimal example to get you started with K-Entity-Framework:

```csharp
using HelloWorld;
using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderContext>(optionsBuilder => optionsBuilder
    // Configure EF Core to use SQL Server
    .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Initial Catalog=Hello World")
    // Enable Kafka extensibility for EF Core (publishing/consuming integration)
    .UseKafkaExtensibility(client => client.BootstrapServers = "localhost:9092"));

using var app = builder.Build();
app.Start();

var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

// here you're intending to mark the entity to be persisted.
dbContext.Orders.Add(new Order { Status = "New" });

// here you're signing the event to be published.
// not a block calling, the event will be published when SaveChangesAsync is called.
dbContext.OrderEvents.Publish(new OrderEvent { Id = 1, Status = Guid.NewGuid().ToString() });

await dbContext.SaveChangesAsync();

// here you're starting to consume kafka and moving the iterator cursor to the next offset in the assigned partitions.
await foreach (var order in dbContext.OrderEvents.WithCancellation(app.Lifetime.ApplicationStopping))
{
    // here you're commiting the offset of the current event.
    await dbContext.SaveChangesAsync();
}

app.WaitForShutdown();

namespace HelloWorld
{
    public class OrderContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; }
        public Topic<OrderEvent> OrderEvents { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }

    public class OrderEvent
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }
}
```

This example demonstrates:
- Setting up a DbContext with Kafka integration
- Publishing events alongside database operations
- Consuming events with automatic offset management
- Transactional consistency between database and Kafka operations

## Core Concepts

### 1. DbContext Integration

K-Entity-Framework extends Entity Framework Core's `DbContext` with Kafka capabilities:

```csharp
public class MyDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public Topic<OrderCreated> OrderEvents { get; set; }
    public Topic<UserRegistered> UserEvents { get; set; }

    public MyDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure your entities
        modelBuilder.Entity<Order>();

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

Topics represent Kafka topics and are configured using a fluent API:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasName("order-events");          // Kafka topic name
        
        topic.UseSystemTextJson(settings =>     // Serialization
        {
            settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        
        topic.HasProducer(producer =>           // Producer configuration
        {
            producer.HasKey(o => o.CustomerId); // Partitioning key
            producer.HasOutbox(outbox =>        // Transactional outbox
            {
                outbox.UseBackgroundOnly();
            });
        });
        
        topic.HasConsumer(consumer =>           // Consumer configuration
        {
            consumer.HasExclusiveConnection(connection =>
            {
                connection.GroupId = "order-processor";
                connection.MaxPollIntervalMs = 300000;
            });
            
            consumer.HasMaxBufferedMessages(2000);
            consumer.HasBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
            
            consumer.HasInbox(inbox =>          // Deduplication
            {
                inbox.HasDeduplicateProperties(o => new { o.OrderId });
                inbox.UseDeduplicationTimeWindow(TimeSpan.FromHours(1));
            });
        });
    });
}
```

### 3. Message Types

Message types are simple C# classes (POCOs):

```csharp
public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Producer Usage

### Basic Producing with Outbox Pattern

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
        // Create the order entity
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Amount = request.Items.Sum(i => i.Price * i.Quantity),
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        // Add to database
        _dbContext.Orders.Add(order);

        // Publish event (stored in outbox for reliable delivery)
        _dbContext.OrderEvents.Publish(new OrderCreated
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = order.Amount,
            CreatedAt = order.CreatedAt,
            Items = request.Items
        });

        // Save both entity and outbox message in same transaction
        await _dbContext.SaveChangesAsync();
    }
}
```

### Immediate Publishing (Without Outbox)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasName("order-events");
        
        topic.HasProducer(producer =>
        {
            producer.HasKey(o => o.CustomerId);
            // No outbox configuration = immediate publishing
        });
    });
}

public async Task CreateOrderAsync(CreateOrderRequest request)
{
    var orderCreated = new OrderCreated
    {
        OrderId = 123,
        CustomerId = request.CustomerId,
        Amount = request.Items.Sum(i => i.Price * i.Quantity),
        CreatedAt = DateTime.UtcNow,
        Items = request.Items
    };

    // Publish immediately to Kafka
    _dbContext.OrderEvents.Publish(orderCreated);
    
    // This will trigger immediate Kafka production
    await _dbContext.SaveChangesAsync();
}
```

## Consumer Usage

### Basic Consuming with Async Enumeration

```csharp
public class OrderEventProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventProcessor> _logger;

    public OrderEventProcessor(IServiceProvider serviceProvider, ILogger<OrderEventProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        try
        {
            // Consume messages using async enumeration
            await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
            {
                try
                {
                    await ProcessOrder(orderEvent);
                    
                    // Commit the message offset and handle deduplication
                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Processed order {OrderId}", orderEvent.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order {OrderId}", orderEvent.OrderId);
                    // Message will not be committed, allowing for retry
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order event processor cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in order event processor");
        }
    }

    private async Task ProcessOrder(OrderCreated orderEvent)
    {
        _logger.LogInformation("Processing order {OrderId} for customer {CustomerId} with amount {Amount}", 
            orderEvent.OrderId, orderEvent.CustomerId, orderEvent.Amount);

        // Your business logic here
        await UpdateInventory(orderEvent.Items);
        await SendConfirmationEmail(orderEvent.CustomerId, orderEvent.OrderId);
        
        _logger.LogInformation("Order {OrderId} processed successfully", orderEvent.OrderId);
    }

    private async Task UpdateInventory(List<OrderItem> items)
    {
        // Update inventory logic
        await Task.Delay(50); // Simulate work
    }

    private async Task SendConfirmationEmail(string customerId, int orderId)
    {
        // Send email logic
        await Task.Delay(50); // Simulate work
    }
}
```

### Error Handling and Retry Logic

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
    {
        var retryCount = 0;
        const int maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                await ProcessOrder(orderEvent);
                await dbContext.SaveChangesAsync();
                break; // Success, exit retry loop
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Attempt {RetryCount}/{MaxRetries} failed for order {OrderId}", 
                    retryCount, maxRetries, orderEvent.OrderId);
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded for order {OrderId}. Sending to dead letter queue", 
                        orderEvent.OrderId);
                    
                    // Send to dead letter queue or handle permanent failure
                    await HandlePermanentFailure(orderEvent, ex);
                    await dbContext.SaveChangesAsync(); // Commit to skip this message
                }
                else
                {
                    // Exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
    }
}
```

## Configuration Patterns

### Service Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register DbContext with Kafka integration - note the chained configuration
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer(connectionString)
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

// Register your consumer
builder.Services.AddHostedService<OrderEventProcessor>();

var app = builder.Build();
```

### Environment-Specific Configuration

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"];
    
    options.UseSqlServer(connectionString)
           .UseKafkaExtensibility(client =>
           {
               client.BootstrapServers = kafkaBootstrapServers;
               
               if (builder.Environment.IsDevelopment())
               {
                   client.Consumer.MaxBufferedMessages = 10; // Lower for development
               }
               else
               {
                   client.Consumer.MaxBufferedMessages = 1000; // Higher for production
                   client.Consumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
               }
           }))
           .AddOutboxKafkaWorker<MyDbContext>(outbox => outbox
               .WithMaxMessagesPerPoll(builder.Environment.IsDevelopment() ? 10 : 100)
               .WithPollingInterval(4000)
               .UseSingleNode());
});
```

## Best Practices

### 1. Message Design

- Keep messages immutable and self-contained
- Include correlation IDs for tracing
- Use meaningful property names
- Avoid circular references

```csharp
public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string Source { get; set; } = "OrderService";
    public int Version { get; set; } = 1;
}
```

### 2. Error Handling

- Implement retry logic with exponential backoff
- Use dead letter queues for permanent failures
- Log detailed error information
- Monitor consumer lag and processing times

### 3. Testing

- Use TestContainers for integration tests
- Mock the DbContext for unit tests
- Test both happy path and error scenarios

```csharp
[Test]
public async Task Should_Publish_Order_Event_When_Order_Created()
{
    // Arrange
    var options = new DbContextOptionsBuilder<MyDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .UseKafkaExtensibility(client => client.BootstrapServers = "localhost:9092")
        .Options;

    using var context = new MyDbContext(options);
    var service = new OrderService(context);

    // Act
    await service.CreateOrderAsync(new CreateOrderRequest
    {
        CustomerId = "customer-123",
        Items = new List<OrderItem>
        {
            new() { ProductId = "product-1", Quantity = 2, Price = 50.00m }
        }
    });

    // Assert
    var order = context.Orders.First();
    Assert.That(order.Amount, Is.EqualTo(100.00m));
    
    // Note: In a real test, you'd verify the message was published
    // This might require additional test infrastructure
}
```

## Next Steps

Now that you understand the basics, explore these advanced topics:

- [Serialization Options](../features/serialization.md) - Configure different serializers
- [Deduplication Strategies](../features/deduplication.md) - Prevent duplicate processing
- [Middleware Configuration](../guides/middleware-configuration.md) - Add custom processing logic
- [Performance Tuning](../guides/performance-tuning.md) - Optimize for high throughput
