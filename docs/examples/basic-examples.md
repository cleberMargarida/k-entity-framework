# Basic Examples

This section provides simple, practical examples to help you get started with K-Entity-Framework quickly.

## Table of Contents

- [Simple Producer and Consumer](#simple-producer-and-consumer)
- [Multiple Message Types](#multiple-message-types)
- [Database Integration](#database-integration)
- [Background Processing](#background-processing)
- [Error Handling](#error-handling)
- [Testing Examples](#testing-examples)

## Simple Producer and Consumer

### Message Definition

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
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### DbContext Setup

```csharp
public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public Topic<OrderCreated> OrderEvents { get; set; }

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
        });

        // Configure OrderCreated topic
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            
            topic.UseSystemTextJson(settings =>
            {
                settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            
            topic.HasProducer(producer =>
            {
                producer.HasKey(o => o.CustomerId);
                producer.HasOutbox();
            });
            
            topic.HasConsumer(consumer =>
            {
                consumer.HasExclusiveConnection(connection =>
                {
                    connection.GroupId = "order-processor";
                });
                
                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(o => new { o.OrderId });
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

### Service Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add DbContext with Kafka integration - note the chained configuration
builder.Services.AddDbContext<OrderDbContext>(options => options
    .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Initial Catalog=OrdersApp")
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        client.Consumer.MaxBufferedMessages = 1000;
    }))
    .AddOutboxKafkaWorker<OrderDbContext>(outbox => outbox
        .WithMaxMessagesPerPoll(100)
        .WithPollingInterval(4000)
        .UseSingleNode());

// Add controllers and background services
builder.Services.AddControllers();
builder.Services.AddHostedService<OrderEventProcessor>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```

### Producer Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderDbContext dbContext, ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        try
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

            _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", 
                order.Id, order.CustomerId);

            return Ok(new { OrderId = order.Id, Status = order.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CreateOrderRequest
{
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

### Consumer Example

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
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        try
        {
            _logger.LogInformation("Starting order event processor");

            await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
            {
                try
                {
                    await ProcessOrderEvent(orderEvent);
                    
                    // Commit the message and handle deduplication
                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully processed order {OrderId}", orderEvent.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order {OrderId}", orderEvent.OrderId);
                    // Don't save changes - message will be retried
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
        _logger.LogInformation("Processing order {OrderId} for customer {CustomerId} with {ItemCount} items", 
            orderEvent.OrderId, orderEvent.CustomerId, orderEvent.Items.Count);

        // Example business logic
        await UpdateInventory(orderEvent.Items);
        await SendConfirmationEmail(orderEvent.CustomerId, orderEvent.OrderId);
        await NotifyFulfillmentTeam(orderEvent);
    }

    private async Task UpdateInventory(List<OrderItem> items)
    {
        foreach (var item in items)
        {
            _logger.LogDebug("Updating inventory for product {ProductId}, quantity: {Quantity}", 
                item.ProductId, item.Quantity);
            
            // Simulate inventory update
            await Task.Delay(10);
        }
    }

    private async Task SendConfirmationEmail(string customerId, int orderId)
    {
        _logger.LogDebug("Sending confirmation email to customer {CustomerId} for order {OrderId}", 
            customerId, orderId);
        
        // Simulate email sending
        await Task.Delay(50);
    }

    private async Task NotifyFulfillmentTeam(OrderCreated orderEvent)
    {
        _logger.LogDebug("Notifying fulfillment team for order {OrderId}", orderEvent.OrderId);
        
        // Simulate fulfillment notification
        await Task.Delay(20);
    }
}
```

## Multiple Message Types

### Message Definitions

```csharp
public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderCancelled
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
    public string Reason { get; set; }
    public DateTime CancelledAt { get; set; }
}

public class PaymentProcessed
{
    public int OrderId { get; set; }
    public string PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

### Multi-Topic DbContext

```csharp
public class OrderManagementDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }
    
    public Topic<OrderCreated> OrderEvents { get; set; }
    public Topic<OrderCancelled> CancellationEvents { get; set; }
    public Topic<PaymentProcessed> PaymentEvents { get; set; }

    public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
        modelBuilder.Entity<Order>();
        modelBuilder.Entity<Payment>();

        // Configure OrderCreated topic
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-created");
            topic.UseSystemTextJson();
            
            topic.HasProducer(producer => producer.HasKey(o => o.CustomerId));
            topic.HasConsumer(consumer => 
            {
                consumer.HasExclusiveConnection(conn => conn.GroupId = "order-management");
            });
        });

        // Configure OrderCancelled topic
        modelBuilder.Topic<OrderCancelled>(topic =>
        {
            topic.HasName("order-cancelled");
            topic.UseSystemTextJson();
            
            topic.HasProducer(producer => producer.HasKey(o => o.CustomerId));
            topic.HasConsumer(consumer => 
            {
                consumer.HasExclusiveConnection(conn => conn.GroupId = "order-management");
            });
        });

        // Configure PaymentProcessed topic
        modelBuilder.Topic<PaymentProcessed>(topic =>
        {
            topic.HasName("payment-processed");
            topic.UseSystemTextJson();
            
            topic.HasProducer(producer => producer.HasKey(p => p.OrderId));
            topic.HasConsumer(consumer => 
            {
                consumer.HasExclusiveConnection(conn => conn.GroupId = "payment-processor");
            });
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

### Multi-Message Consumer

```csharp
public class OrderManagementProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderManagementProcessor> _logger;

    public OrderManagementProcessor(IServiceProvider serviceProvider, ILogger<OrderManagementProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

        // Start multiple tasks to process different message types concurrently
        var tasks = new[]
        {
            ProcessOrderEvents(dbContext, stoppingToken),
            ProcessCancellationEvents(dbContext, stoppingToken),
            ProcessPaymentEvents(dbContext, stoppingToken)
        };

        await Task.WhenAll(tasks);
    }

    private async Task ProcessOrderEvents(OrderManagementDbContext dbContext, CancellationToken stoppingToken)
    {
        await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing order created: {OrderId}", orderEvent.OrderId);
                
                // Handle order created logic
                await HandleOrderCreated(orderEvent);
                
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order created event: {OrderId}", orderEvent.OrderId);
            }
        }
    }

    private async Task ProcessCancellationEvents(OrderManagementDbContext dbContext, CancellationToken stoppingToken)
    {
        await foreach (var cancellationEvent in dbContext.CancellationEvents.WithCancellation(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing order cancellation: {OrderId}", cancellationEvent.OrderId);
                
                // Handle order cancellation logic
                await HandleOrderCancellation(cancellationEvent);
                
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order cancellation event: {OrderId}", cancellationEvent.OrderId);
            }
        }
    }

    private async Task ProcessPaymentEvents(OrderManagementDbContext dbContext, CancellationToken stoppingToken)
    {
        await foreach (var paymentEvent in dbContext.PaymentEvents.WithCancellation(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing payment: {PaymentId} for order {OrderId}", 
                    paymentEvent.PaymentId, paymentEvent.OrderId);
                
                // Handle payment processed logic
                await HandlePaymentProcessed(paymentEvent);
                
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment event: {PaymentId}", paymentEvent.PaymentId);
            }
        }
    }

    private async Task HandleOrderCreated(OrderCreated orderEvent)
    {
        // Business logic for order creation
        await Task.Delay(50); // Simulate work
    }

    private async Task HandleOrderCancellation(OrderCancelled cancellationEvent)
    {
        // Business logic for order cancellation
        await Task.Delay(50); // Simulate work
    }

    private async Task HandlePaymentProcessed(PaymentProcessed paymentEvent)
    {
        // Business logic for payment processing
        await Task.Delay(50); // Simulate work
    }
}
```
            Amount = request.Items.Sum(i => i.Price * i.Quantity),
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };

        // Publish event (stored in outbox for reliable delivery)
        _dbContext.OrderEvents.Publish(orderCreated);
        
        // Save both entity and outbox message in same transaction
        await _dbContext.SaveChangesAsync();

        return Ok(new { OrderId = order.OrderId });
    }
}

public class CreateOrderRequest
{
    public string CustomerId { get; set; }
    public List<CreateOrderItem> Items { get; set; } = new();
}

public class CreateOrderItem
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### Consumer Example

```csharp
[BackgroundService]
public class OrderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(IServiceProvider serviceProvider, ILogger<OrderProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        try
        {
            // Consume messages using async enumeration
            await foreach (var orderEvent in dbContext.OrderEvents.WithCancellation(stoppingToken))
            {
                try
                {
                    await ProcessOrder(orderEvent);
                    
                    // Commit the message and handle deduplication
                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully processed order {OrderId}", orderEvent.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order {OrderId}", orderEvent.OrderId);
                    // Don't save changes - message will be retried
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

    private async Task ProcessOrder(OrderCreated order)
    {
        // Simulate order processing
        _logger.LogInformation("Processing order {OrderId} for customer {CustomerId} with amount {Amount:C}", 
            order.OrderId, order.CustomerId, order.Amount);

        // Example processing logic
        foreach (var item in order.Items)
        {
            _logger.LogInformation("Processing item: {ProductName} x {Quantity}", 
                item.ProductName, item.Quantity);
            
            // Update inventory, send notifications, etc.
            await Task.Delay(100); // Simulate processing time
        }

        _logger.LogInformation("Order {OrderId} processed successfully", order.OrderId);
    }
}

// Register the background service
builder.Services.AddHostedService<OrderProcessor>();
```

## Multiple Message Types

### Multiple Message Definitions

```csharp
public class OrderCreated
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderCancelled
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string Reason { get; set; }
    public DateTime CancelledAt { get; set; }
}

public class PaymentProcessed
{
    public string PaymentId { get; set; }
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

### Multi-Topic DbContext

```csharp
public class EcommerceDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }
    
    public Topic<OrderCreated> OrderEvents { get; set; }
    public Topic<OrderCancelled> CancellationEvents { get; set; }
    public Topic<PaymentProcessed> PaymentEvents { get; set; }

    public EcommerceDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.HasProducer();
            topic.HasConsumer(consumer => consumer.WithGroupId("order-processor"));
        });

        modelBuilder.Topic<OrderCancelled>(topic =>
        {
            topic.HasName("order-cancellation-events");
            topic.HasProducer();
            topic.HasConsumer(consumer => consumer.WithGroupId("cancellation-processor"));
        });

        modelBuilder.Topic<PaymentProcessed>(topic =>
        {
            topic.HasName("payment-events");
            topic.HasProducer();
            topic.HasConsumer(consumer => consumer.WithGroupId("payment-processor"));
        });
    }
}
```

### Multi-Type Consumer

```csharp
[BackgroundService]
public class EcommerceEventProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EcommerceEventProcessor> _logger;

    public EcommerceEventProcessor(IServiceProvider serviceProvider, ILogger<EcommerceEventProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new[]
        {
            ProcessOrderEvents(stoppingToken),
            ProcessCancellationEvents(stoppingToken),
            ProcessPaymentEvents(stoppingToken)
        };

        await Task.WhenAll(tasks);
    }

    private async Task ProcessOrderEvents(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

            try
            {
                var result = await brokerContext.OrderEvents.FirstAsync();
                if (result != null)
                {
                    await ProcessOrderCreated(result.Message);
                    await brokerContext.OrderEvents.CommitAsync();
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order event");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessCancellationEvents(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

            try
            {
                var result = await brokerContext.CancellationEvents.FirstAsync();
                if (result != null)
                {
                    await ProcessOrderCancellation(result.Message);
                    await brokerContext.CancellationEvents.CommitAsync();
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cancellation event");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessPaymentEvents(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

            try
            {
                var result = await brokerContext.PaymentEvents.FirstAsync();
                if (result != null)
                {
                    await ProcessPayment(result.Message);
                    await brokerContext.PaymentEvents.CommitAsync();
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment event");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessOrderCreated(OrderCreated order)
    {
        _logger.LogInformation("Processing order created: {OrderId}", order.OrderId);
        // Business logic for order creation
        await Task.Delay(100);
    }

    private async Task ProcessOrderCancellation(OrderCancelled cancellation)
    {
        _logger.LogInformation("Processing order cancellation: {OrderId}", cancellation.OrderId);
        // Business logic for order cancellation
        await Task.Delay(100);
    }

    private async Task ProcessPayment(PaymentProcessed payment)
    {
        _logger.LogInformation("Processing payment: {PaymentId} for order {OrderId}", 
            payment.PaymentId, payment.OrderId);
        // Business logic for payment processing
        await Task.Delay(100);
    }
}
```

## Database Integration

### Entity Models

```csharp
public class Order
{
    public string Id { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public string OrderId { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderStatus
{
    Created,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
```

### DbContext with Kafka Integration

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey(e => e.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
```

### Service with Database and Kafka

```csharp
public class OrderService
{
    private readonly AppDbContext _dbContext;
    private readonly EcommerceBrokerContext _brokerContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext dbContext, 
        EcommerceBrokerContext brokerContext,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _brokerContext = brokerContext;
        _logger = logger;
    }

    public async Task<string> CreateOrderAsync(CreateOrderRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // Create order in database
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = request.CustomerId,
                Amount = request.Items.Sum(i => i.Price * i.Quantity),
                Status = OrderStatus.Created,
                CreatedAt = DateTime.UtcNow,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            // Publish event to Kafka
            var orderEvent = new OrderCreated
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Amount = order.Amount,
                CreatedAt = order.CreatedAt
            };

            await _brokerContext.OrderEvents.ProduceAsync(orderEvent);

            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
            return order.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create order");
            throw;
        }
    }

    public async Task CancelOrderAsync(string orderId, string reason)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order {orderId} not found");
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // Update order in database
            order.Status = OrderStatus.Cancelled;
            await _dbContext.SaveChangesAsync();

            // Publish cancellation event
            var cancellationEvent = new OrderCancelled
            {
                OrderId = orderId,
                CustomerId = order.CustomerId,
                Reason = reason,
                CancelledAt = DateTime.UtcNow
            };

            await _brokerContext.CancellationEvents.ProduceAsync(cancellationEvent);

            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} cancelled: {Reason}", orderId, reason);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            throw;
        }
    }
}
```

## Background Processing

### Batch Processing Consumer

```csharp
[BackgroundService]
public class BatchOrderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BatchOrderProcessor> _logger;

    public BatchOrderProcessor(IServiceProvider serviceProvider, ILogger<BatchOrderProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

            try
            {
                // Process messages in batches of 10
                var results = await brokerContext.OrderEvents.TakeAsync(10);
                
                if (results.Any())
                {
                    await ProcessOrderBatch(results.Select(r => r.Message));
                    await brokerContext.OrderEvents.CommitAsync();
                    
                    _logger.LogInformation("Processed batch of {Count} orders", results.Count());
                }
                else
                {
                    await Task.Delay(5000, stoppingToken); // Wait when no messages
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order batch");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task ProcessOrderBatch(IEnumerable<OrderCreated> orders)
    {
        var tasks = orders.Select(ProcessOrder);
        await Task.WhenAll(tasks);
    }

    private async Task ProcessOrder(OrderCreated order)
    {
        _logger.LogInformation("Processing order {OrderId}", order.OrderId);
        
        // Simulate processing
        await Task.Delay(100);
        
        _logger.LogInformation("Order {OrderId} processed", order.OrderId);
    }
}
```

### Parallel Processing Consumer

```csharp
[BackgroundService]
public class ParallelOrderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ParallelOrderProcessor> _logger;
    private readonly SemaphoreSlim _semaphore;

    public ParallelOrderProcessor(IServiceProvider serviceProvider, ILogger<ParallelOrderProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount); // Limit concurrency
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = Enumerable.Range(0, Environment.ProcessorCount)
                             .Select(_ => ProcessMessages(stoppingToken));
        
        await Task.WhenAll(tasks);
    }

    private async Task ProcessMessages(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

            try
            {
                var result = await brokerContext.OrderEvents.FirstAsync();
                
                if (result != null)
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        await ProcessOrder(result.Message);
                        await brokerContext.OrderEvents.CommitAsync();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessOrder(OrderCreated order)
    {
        _logger.LogInformation("Processing order {OrderId} on thread {ThreadId}", 
            order.OrderId, Thread.CurrentThread.ManagedThreadId);
        
        // Simulate CPU-intensive processing
        await Task.Run(() =>
        {
            Thread.Sleep(1000); // Simulate work
        });
        
        _logger.LogInformation("Order {OrderId} processed", order.OrderId);
    }
}
```

## Error Handling

### Retry with Exponential Backoff

```csharp
[BackgroundService]
public class ResilientOrderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResilientOrderProcessor> _logger;

    public ResilientOrderProcessor(IServiceProvider serviceProvider, ILogger<ResilientOrderProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

            try
            {
                var result = await brokerContext.OrderEvents.FirstAsync();
                
                if (result != null)
                {
                    await ProcessOrderWithRetry(result.Message, 3);
                    await brokerContext.OrderEvents.CommitAsync();
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in order processor");
                await Task.Delay(30000, stoppingToken); // Long delay for fatal errors
            }
        }
    }

    private async Task ProcessOrderWithRetry(OrderCreated order, int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await ProcessOrder(order);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                _logger.LogWarning(ex, "Failed to process order {OrderId}, attempt {Attempt}/{MaxRetries}. Retrying in {Delay}", 
                    order.OrderId, attempt, maxRetries, delay);
                
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process order {OrderId} after {MaxRetries} attempts", 
                    order.OrderId, maxRetries);
                throw; // Re-throw after all retries exhausted
            }
        }
    }

    private async Task ProcessOrder(OrderCreated order)
    {
        // Simulate processing that might fail
        if (Random.Shared.NextDouble() < 0.3) // 30% failure rate
        {
            throw new InvalidOperationException("Simulated processing failure");
        }

        _logger.LogInformation("Processing order {OrderId}", order.OrderId);
        await Task.Delay(500); // Simulate work
        _logger.LogInformation("Order {OrderId} processed successfully", order.OrderId);
    }
}
```

### Dead Letter Queue Pattern

```csharp
public class DeadLetterOrderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadLetterOrderProcessor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();
            var deadLetterService = scope.ServiceProvider.GetRequiredService<IDeadLetterService>();

            try
            {
                var result = await brokerContext.OrderEvents.FirstAsync();
                
                if (result != null)
                {
                    try
                    {
                        await ProcessOrder(result.Message);
                        await brokerContext.OrderEvents.CommitAsync();
                    }
                    catch (BusinessException ex)
                    {
                        // Business logic error - send to dead letter queue
                        _logger.LogError(ex, "Business error processing order {OrderId}", result.Message.OrderId);
                        await deadLetterService.SendToDeadLetterAsync(result.Message, ex.Message);
                        await brokerContext.OrderEvents.CommitAsync(); // Commit to avoid reprocessing
                    }
                    catch (TransientException ex)
                    {
                        // Transient error - don't commit, will be retried
                        _logger.LogWarning(ex, "Transient error processing order {OrderId}", result.Message.OrderId);
                        await Task.Delay(5000, stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in order processor");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task ProcessOrder(OrderCreated order)
    {
        // Validate order
        if (string.IsNullOrEmpty(order.CustomerId))
        {
            throw new BusinessException("Customer ID is required");
        }

        if (order.Amount <= 0)
        {
            throw new BusinessException("Order amount must be positive");
        }

        // Simulate transient errors (database timeouts, network issues)
        if (Random.Shared.NextDouble() < 0.1) // 10% transient failure rate
        {
            throw new TransientException("Database timeout");
        }

        _logger.LogInformation("Processing order {OrderId}", order.OrderId);
        await Task.Delay(200);
        _logger.LogInformation("Order {OrderId} processed", order.OrderId);
    }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
}

public interface IDeadLetterService
{
    Task SendToDeadLetterAsync<T>(T message, string reason);
}
```

## Testing Examples

### Unit Testing Producer

```csharp
[Test]
public async Task CreateOrder_ShouldProduceOrderCreatedEvent()
{
    // Arrange
    var mockBrokerContext = new Mock<EcommerceBrokerContext>();
    var mockOrderTopic = new Mock<Topic<OrderCreated>>();
    
    mockBrokerContext.Setup(x => x.OrderEvents).Returns(mockOrderTopic.Object);
    
    var orderService = new OrderService(mockDbContext.Object, mockBrokerContext.Object, mockLogger.Object);
    
    var request = new CreateOrderRequest
    {
        CustomerId = "customer-123",
        Items = new List<CreateOrderItem>
        {
            new() { ProductId = "product-1", ProductName = "Product 1", Quantity = 2, Price = 10.00m }
        }
    };

    // Act
    await orderService.CreateOrderAsync(request);

    // Assert
    mockOrderTopic.Verify(x => x.ProduceAsync(It.Is<OrderCreated>(order => 
        order.CustomerId == "customer-123" && 
        order.Amount == 20.00m)), Times.Once);
}
```

### Integration Testing

```csharp
[Test]
public async Task OrderProcessing_EndToEnd_ShouldWork()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddDbContext<AppDbContext>(options => 
        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    services.AddBrokerContext<EcommerceBrokerContext>();
    
    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    
    var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
    var brokerContext = scope.ServiceProvider.GetRequiredService<EcommerceBrokerContext>();

    // Act
    var orderId = await orderService.CreateOrderAsync(new CreateOrderRequest
    {
        CustomerId = "test-customer",
        Items = new List<CreateOrderItem>
        {
            new() { ProductId = "test-product", ProductName = "Test Product", Quantity = 1, Price = 50.00m }
        }
    });

    // Consume the message
    var result = await brokerContext.OrderEvents.FirstAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(orderId, result.Message.OrderId);
    Assert.Equal("test-customer", result.Message.CustomerId);
    Assert.Equal(50.00m, result.Message.Amount);
}
```

## Next Steps

- [Advanced Examples](advanced-examples.md) - More complex scenarios and patterns
- [Integration Examples](integration-examples.md) - Real-world integration patterns
- [Performance Examples](performance-examples.md) - High-performance configurations
