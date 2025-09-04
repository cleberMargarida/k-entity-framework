# Basic Examples

This section provides simple, practical examples to help you get started with K-Entity-Framework quickly.

## Simple Producer and Consumer

### DbContext Setup

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
            topic.HasConsumer(consumer => consumer.HasExclusiveConnection(c => c.GroupId = "order-processor"));
        });
    }
}

public class OrderCreated
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; }
}
```

### Service Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options => options
    .UseInMemoryDatabase("orders-db")
    .UseKafkaExtensibility(client => client.BootstrapServers = "localhost:9092"));

builder.Services.AddHostedService<OrderEventProcessor>();

var app = builder.Build();
app.Run();
```

### Producer Example

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
        _dbContext.OrderEvents.Publish(new OrderCreated { OrderId = 1, CustomerId = "123" });
        await _dbContext.SaveChangesAsync();
    }
}
```

### Consumer Example

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