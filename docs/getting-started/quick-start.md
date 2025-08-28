# Quick Start Guide

Get up and running with K-Entity-Framework in just a few minutes.

## Prerequisites

- .NET 6.0 or later
- Apache Kafka cluster (local or cloud)
- Entity Framework Core knowledge (recommended)

## Installation

Add the K-Entity-Framework package to your project:

```bash
dotnet add package K.EntityFrameworkCore
```

## Basic Setup

### 1. Define Your Message Types

```csharp
public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderCancelled
{
    public int OrderId { get; set; }
    public string Reason { get; set; }
    public DateTime CancelledAt { get; set; }
}
```

### 2. Create Your DbContext with Kafka Integration

```csharp
public class MyDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public Topic<OrderCreated> OrderEvents { get; set; }
    public Topic<OrderCancelled> CancellationEvents { get; set; }

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired();
        });

        // Configure OrderCreated topic
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-created-events");
            
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
                    connection.GroupId = "order-processor-group";
                    connection.MaxPollIntervalMs = 300000;
                });
                
                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(o => new { o.OrderId });
                    inbox.UseDeduplicationTimeWindow(TimeSpan.FromHours(1));
                });
            });
        });

        // Configure OrderCancelled topic
        modelBuilder.Topic<OrderCancelled>(topic =>
        {
            topic.HasName("order-cancelled-events");
            topic.UseSystemTextJson();
            
            topic.HasProducer(producer =>
            {
                producer.HasKey(o => o.OrderId);
            });
            
            topic.HasConsumer(consumer =>
            {
                consumer.HasExclusiveConnection(conn => 
                {
                    conn.GroupId = "cancellation-processor-group";
                });
            });
        });

        base.OnModelCreating(modelBuilder);
    }
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

### 3. Configure Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add DbContext with Kafka integration - note the chained configuration
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Initial Catalog=MyApp")
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

## Your First Producer

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MyDbContext _dbContext;

    public OrdersController(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        // Create the order entity
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        // Add to database
        _dbContext.Orders.Add(order);

        // Publish event to Kafka (stored in outbox for reliable delivery)
        _dbContext.OrderEvents.Publish(new OrderCreated
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = order.Amount,
            Status = order.Status,
            CreatedAt = order.CreatedAt
        });

        // Save changes (both entity and outbox message)
        await _dbContext.SaveChangesAsync();

        return Ok(new { OrderId = order.Id });
    }
}

public class CreateOrderRequest
{
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
}
```

## Your First Consumer

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
                    // Process the order event
                    await ProcessOrderEvent(orderEvent);
                    
                    // Commit the message by saving changes (handles offset commit and deduplication)
                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully processed order event: {OrderId}", orderEvent.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order event: {OrderId}", orderEvent.OrderId);
                    // Optionally implement retry logic or dead letter queue here
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

    private async Task ProcessOrderEvent(OrderCreated orderEvent)
    {
        // Your business logic here
        _logger.LogInformation("Processing order {OrderId} for customer {CustomerId} with amount {Amount}", 
            orderEvent.OrderId, orderEvent.CustomerId, orderEvent.Amount);
        
        // Example: Send confirmation email, update inventory, trigger fulfillment, etc.
        await Task.Delay(100); // Simulate processing time
    }
}
```

## Register the Background Service

```csharp
var builder = WebApplication.CreateBuilder(args);

// ... existing service configuration ...

// Register the background service
builder.Services.AddHostedService<OrderEventProcessor>();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.MapControllers();

app.Run();
```

## Running Your Application

1. **Start Kafka** (if running locally):
   ```bash
   docker run -d --name kafka -p 9092:9092 \
     -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 \
     -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092 \
     -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
     confluentinc/cp-kafka:latest
   ```

2. **Run your application**:
   ```bash
   dotnet run
   ```

3. **Test the producer** by making a POST request to `/api/orders`:
   ```json
   {
     "customerId": "customer-123",
     "amount": 99.99
   }
   ```

4. **Check the console** to see the consumer processing the message.

## What's Next?

Now that you have a basic setup working, explore these advanced features:

- [Serialization](../features/serialization.md) - Configure JSON, MessagePack, or custom serializers
- [Deduplication](../features/deduplication.md) - Prevent duplicate message processing
- [Outbox Pattern](../features/outbox.md) - Ensure reliable message publishing with database transactions
- [Type-Specific Processing](../features/type-specific-processing.md) - Configure different processing options per message type
- [Middleware Configuration](../guides/middleware-configuration.md) - Add retry logic, error handling, and more

## Common Issues

### Connection Errors
- Ensure Kafka is running and accessible at the configured bootstrap servers
- Check firewall settings and network connectivity
- Verify the correct port (default is 9092)

### Serialization Errors
- Ensure your message classes have parameterless constructors
- Check that all properties have public getters and setters
- Consider configuring custom serialization options if needed

### Consumer Not Processing Messages
- Verify the topic name matches between producer and consumer configuration
- Check that the consumer group settings allow message consumption
- Ensure the background service is properly registered and running
- Make sure to call `SaveChangesAsync()` to commit message offsets

### Outbox Messages Not Being Processed
- Ensure the outbox worker is registered with `AddOutboxKafkaWorker<T>()`
- Check that topics are configured with `.HasOutbox()` on the producer
- Verify the database contains the outbox tables (they should be auto-created)
