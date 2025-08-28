# Basic Usage

Learn the fundamental concepts and usage patterns of K-Entity-Framework.

## Core Concepts

### 1. DbContext Integration

K-Entity-Framework extends Entity Framework Core's `DbContext` with Kafka capabilities:

```csharp
public class MyDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public Topic<OrderCreated> OrderEvents { get; set; }
    public Topic<UserRegistered> UserEvents { get; set; }

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

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

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            // Consume multiple messages at once
            var results = await brokerContext.OrderEvents.TakeAsync(10); // Take up to 10 messages
            
            if (results.Any())
            {
                await ProcessOrdersBatch(results);
                
                // Commit all messages at once
                await brokerContext.OrderEvents.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order batch");
            await Task.Delay(5000, stoppingToken);
        }
    }
}

private async Task ProcessOrdersBatch(IEnumerable<ConsumeResult<OrderCreated>> orders)
{
    var tasks = orders.Select(ProcessOrder);
    await Task.WhenAll(tasks);
}
```

### Consumer with Manual Commit

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var scope = _serviceProvider.CreateScope();
    var brokerContext = scope.ServiceProvider.GetRequiredService<MyBrokerContext>();

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var result = await brokerContext.OrderEvents.FirstAsync();
            
            if (result != null)
            {
                try
                {
                    await ProcessOrder(result);
                    
                    // Only commit if processing was successful
                    await brokerContext.OrderEvents.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process order {OrderId}, will retry", 
                        result.Message.OrderId);
                    
                    // Don't commit - this will cause the message to be redelivered
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in consumer loop");
            await Task.Delay(5000, stoppingToken);
        }
    }
}
```

## Configuration

### Service Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add Entity Framework with Kafka integration
    services.AddDbContext<MyDbContext>(options =>
        options.UseSqlServer(connectionString)
               .UseKafkaExtensibility(kafka =>
               {
                   kafka.BootstrapServers = "localhost:9092";
                   kafka.ClientId = "my-application";
                   
                   // Configure producer settings
                   kafka.Producer.Acks = Acks.All;
                   kafka.Producer.EnableIdempotence = true;
                   
                   // Configure consumer settings
                   kafka.Consumer.GroupId = "my-consumer-group";
                   kafka.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
               }));

    // Add broker context
    services.AddBrokerContext<MyBrokerContext>();
    
    // Add background services
    services.AddHostedService<OrderEventProcessor>();
}
```

### Topic Configuration

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasName("order-events");
        
        // Producer configuration
        topic.HasProducer(producer =>
        {
            producer.WithKey(order => order.CustomerId); // Partition by customer
        });
        
        // Consumer configuration
        topic.HasConsumer(consumer =>
        {
            consumer.WithGroupId("order-processor");
            consumer.WithAutoOffsetReset(AutoOffsetReset.Earliest);
        });
        
        // Enable serialization
        topic.UseJsonSerializer(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.WriteIndented = false;
        });
    });
}
```

## Error Handling

### Consumer Error Handling

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var scope = _serviceProvider.CreateScope();
    var brokerContext = scope.ServiceProvider.GetRequiredService<MyBrokerContext>();

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var result = await brokerContext.OrderEvents.FirstAsync();
            
            if (result != null)
            {
                try
                {
                    await ProcessOrder(result);
                    await brokerContext.OrderEvents.CommitAsync();
                }
                catch (BusinessLogicException ex)
                {
                    // Business logic error - log and skip this message
                    _logger.LogError(ex, "Business logic error for order {OrderId}", 
                        result.Message.OrderId);
                    
                    await brokerContext.OrderEvents.CommitAsync(); // Skip this message
                }
                catch (TransientException ex)
                {
                    // Transient error - retry later
                    _logger.LogWarning(ex, "Transient error for order {OrderId}, will retry", 
                        result.Message.OrderId);
                    
                    // Don't commit - message will be redelivered
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
        catch (ConsumeException ex)
        {
            // Kafka-specific errors
            _logger.LogError(ex, "Kafka consume error: {Error}", ex.Error.Reason);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (Exception ex)
        {
            // Unexpected errors
            _logger.LogError(ex, "Unexpected error in consumer");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

## Best Practices

### 1. Use Scoped Services

Always create a new scope for each message processing cycle:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        using var scope = _serviceProvider.CreateScope();
        var brokerContext = scope.ServiceProvider.GetRequiredService<MyBrokerContext>();
        
        // Process messages...
    }
}
```

### 2. Handle Cancellation Properly

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Processing logic...
            
            // Check cancellation periodically
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when service is stopping
        _logger.LogInformation("Consumer service is stopping");
    }
}
```

### 3. Use Structured Logging

```csharp
private async Task ProcessOrder(ConsumeResult<OrderCreated> result)
{
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["OrderId"] = result.Message.OrderId,
        ["CustomerId"] = result.Message.CustomerId,
        ["Partition"] = result.Partition.Value,
        ["Offset"] = result.Offset.Value
    });

    _logger.LogInformation("Processing order");
    
    // Processing logic...
    
    _logger.LogInformation("Order processed successfully");
}
```

## Next Steps

- [Serialization](../features/serialization.md) - Configure different serialization formats
- [Middleware Configuration](../guides/middleware-configuration.md) - Add retry logic and error handling
- [Performance Tuning](../guides/performance-tuning.md) - Optimize for high throughput
