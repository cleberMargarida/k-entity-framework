# Debezium Complete Example

This guide provides a complete, end-to-end working example of K-Entity-Framework with Debezium integration.

## Project Overview

We'll build an order processing system that:
- Creates orders and stores them in the database
- Publishes `OrderCreated` events via the outbox pattern
- Uses Debezium to stream events to Kafka in real-time
- Consumes events and processes them asynchronously

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop
- SQL Server (or use Docker image)
- Your favorite IDE (Visual Studio, VS Code, Rider)

## Project Setup

### 1. Create the Project

```powershell
# Create solution and projects
dotnet new sln -n OrderService
dotnet new webapi -n OrderService.Api
dotnet new console -n OrderService.Consumer
dotnet sln add OrderService.Api OrderService.Consumer

cd OrderService.Api
```

### 2. Install NuGet Packages

```powershell
# API Project
dotnet add package K.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Confluent.Kafka

# Consumer Project (in OrderService.Consumer directory)
cd ..\OrderService.Consumer
dotnet add package K.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Confluent.Kafka
dotnet add package Microsoft.Extensions.Hosting
```

## Implementation

### 3. Define Event Models

Create `Events/OrderCreated.cs`:

```csharp
namespace OrderService.Api.Events;

public class OrderCreated
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### 4. Create DbContext

Create `Data/OrderDbContext.cs`:

```csharp
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Events;
using OrderService.Api.Models;

namespace OrderService.Api.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.OwnsMany(e => e.Items, items =>
            {
                items.Property(i => i.Price).HasPrecision(18, 2);
            });
        });

        // Configure Kafka topic with outbox
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.HasProducer(producer =>
            {
                producer.HasKey(order => order.OrderId.ToString());
                producer.HasOutbox(); // Enable outbox pattern
            });
        });
    }
}
```

### 5. Create Order Model

Create `Models/Order.cs`:

```csharp
namespace OrderService.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### 6. Configure Services

Update `Program.cs`:

```csharp
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Kafka
builder.Services.AddKafka(options =>
{
    options.ConfigureClient(client =>
    {
        client.BootstrapServers = "localhost:9092";
    });

    options.ConfigureProducer<OrderCreated>(producer =>
    {
        producer.Acks = Confluent.Kafka.Acks.All;
        producer.EnableIdempotence = true;
    });
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 7. Create API Controller

Create `Controllers/OrdersController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Events;
using OrderService.Api.Models;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderDbContext dbContext,
        ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Create order entity
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new Models.OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.Price * i.Quantity);

        // Save to database
        _dbContext.Orders.Add(order);

        // Produce event to outbox (will be published by Debezium)
        await _dbContext.OrderCreated.ProduceAsync(new OrderCreated
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new Events.OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        });

        // Save all changes in a single transaction
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created order {OrderId} for customer {CustomerId}", 
            order.Id, order.CustomerId);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return order;
    }

    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetOrders()
    {
        return await _dbContext.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}

public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public List<CreateOrderItem> Items { get; set; } = new();
}

public class CreateOrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### 8. Add Configuration

Create/update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OrderServiceDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 9. Create Consumer Application

Create `OrderService.Consumer/Program.cs`:

```csharp
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderService.Api.Data;
using OrderService.Api.Events;

var builder = Host.CreateApplicationBuilder(args);

// Configure DbContext
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Kafka consumer
builder.Services.AddKafka(options =>
{
    options.ConfigureClient(client =>
    {
        client.BootstrapServers = "localhost:9092";
    });

    options.ConfigureConsumer<OrderCreated>(consumer =>
    {
        consumer.GroupId = "order-processor";
        consumer.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
        consumer.EnableAutoCommit = false;
    });
});

var host = builder.Build();

// Consume messages
using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

await foreach (var message in dbContext.OrderCreated.ConsumeAsync())
{
    try
    {
        Console.WriteLine($"Received order {message.OrderId}:");
        Console.WriteLine($"  Customer: {message.CustomerId}");
        Console.WriteLine($"  Total: ${message.TotalAmount:F2}");
        Console.WriteLine($"  Items: {message.Items.Count}");
        
        foreach (var item in message.Items)
        {
            Console.WriteLine($"    - {item.ProductId}: {item.Quantity} x ${item.Price:F2}");
        }
        
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing order {message.OrderId}: {ex.Message}");
    }
}
```

Add `appsettings.json` to Consumer project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OrderServiceDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=true"
  }
}
```

## Infrastructure Setup

### 10. Docker Compose

Create `docker-compose.yml` in solution root:

```yaml
version: '3.8'
services:
  kafka:
    image: confluentinc/confluent-local:7.7.1
    ports: 
      - "9092:9092"
      - "9093:9093"
    environment:
      KAFKA_LISTENERS: PLAINTEXT://localhost:29092,CONTROLLER://localhost:29093,PLAINTEXT_HOST://0.0.0.0:9092,PLAINTEXT_INTERNAL://0.0.0.0:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:9092,PLAINTEXT_INTERNAL://kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1

  kafka-connect:
    image: debezium/connect:2.5
    depends_on: [kafka]
    ports: ["8083:8083"]
    environment:
      BOOTSTRAP_SERVERS: kafka:9093
      GROUP_ID: debezium-cluster
      CONFIG_STORAGE_TOPIC: connect-configs
      OFFSET_STORAGE_TOPIC: connect-offsets
      STATUS_STORAGE_TOPIC: connect-status
      CONFIG_STORAGE_REPLICATION_FACTOR: 1
      OFFSET_STORAGE_REPLICATION_FACTOR: 1
      STATUS_STORAGE_REPLICATION_FACTOR: 1
```

### 11. Enable SQL Server CDC

```sql
-- Enable CDC on database
USE OrderServiceDb;
GO

EXEC sys.sp_cdc_enable_db;
GO

-- Enable CDC on outbox table
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'outbox_messages',
    @role_name = NULL,
    @supports_net_changes = 1;
GO
```

### 12. Deploy Debezium Connector

Create `connector.json`:

```json
{
  "name": "order-outbox-connector",
  "config": {
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "database.hostname": "host.docker.internal",
    "database.port": "1433",
    "database.user": "sa",
    "database.password": "YourStrong@Password",
    "database.dbname": "OrderServiceDb",
    "database.server.name": "orderserver",
    "table.include.list": "dbo.outbox_messages",
    "database.encrypt": "false",
    "database.trustServerCertificate": "true",
    
    "transforms": "outbox",
    "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
    "transforms.outbox.table.field.event.id": "Id",
    "transforms.outbox.table.field.event.key": "AggregateId",
    "transforms.outbox.table.field.event.type": "Type",
    "transforms.outbox.table.field.payload": "Payload",
    "transforms.outbox.route.by.field": "Topic",
    
    "key.converter": "org.apache.kafka.connect.storage.StringConverter",
    "value.converter": "org.apache.kafka.connect.json.JsonConverter",
    "value.converter.schemas.enable": "false"
  }
}
```

Deploy:

```powershell
curl -X POST http://localhost:8083/connectors `
  -H "Content-Type: application/json" `
  -d '@connector.json'
```

## Running the Example

### 13. Start Infrastructure

```powershell
# Start Kafka and Kafka Connect
docker-compose up -d

# Wait for services to be ready
Start-Sleep -Seconds 10

# Deploy connector
curl -X POST http://localhost:8083/connectors `
  -H "Content-Type: application/json" `
  -d '@connector.json'
```

### 14. Run Applications

```powershell
# Terminal 1 - API
cd OrderService.Api
dotnet run

# Terminal 2 - Consumer
cd OrderService.Consumer
dotnet run
```

### 15. Create an Order

```powershell
curl -X POST http://localhost:5000/api/orders `
  -H "Content-Type: application/json" `
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "items": [
      {
        "productId": "PROD-001",
        "quantity": 2,
        "price": 29.99
      },
      {
        "productId": "PROD-002",
        "quantity": 1,
        "price": 49.99
      }
    ]
  }'
```

### 16. Verify Event Flow

You should see:
1. API logs: "Created order {OrderId}..."
2. Consumer logs: "Received order {OrderId}..."

The event flows through:
1. API saves to database and outbox table
2. Debezium captures change from CDC
3. EventRouter transforms to Kafka message
4. Message published to `order-events` topic
5. Consumer receives and processes message

## Monitoring

### Check Outbox Table

```sql
SELECT TOP 10 * FROM dbo.outbox_messages ORDER BY CreatedAt DESC;
```

### Check Kafka Topics

```powershell
docker exec -it kafka kafka-topics --bootstrap-server localhost:9092 --list
docker exec -it kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic order-events --from-beginning
```

### Check Connector Status

```powershell
curl http://localhost:8083/connectors/order-outbox-connector/status
```

## Cleanup

```powershell
# Stop applications (Ctrl+C in terminals)

# Stop infrastructure
docker-compose down

# Clean database
# DROP DATABASE OrderServiceDb;
```

## Next Steps

- Add error handling and retries in consumer
- Implement dead-letter queue for failed messages
- Add monitoring with Application Insights or Prometheus
- Deploy to production with [Aspire](debezium-aspire.md)
- Explore [PostgreSQL setup](debezium-postgresql.md) as an alternative

## Troubleshooting

### Events Not Appearing in Kafka

1. Check CDC is enabled: `SELECT is_cdc_enabled FROM sys.databases WHERE name = 'OrderServiceDb'`
2. Check connector status: `curl http://localhost:8083/connectors/order-outbox-connector/status`
3. Check connector logs: `docker logs kafka-connect --tail 100`
4. Verify outbox table has records: `SELECT * FROM dbo.outbox_messages`

### Consumer Not Receiving Messages

1. Check Kafka topic exists: `docker exec -it kafka kafka-topics --list --bootstrap-server localhost:9092`
2. Verify messages in topic: `docker exec -it kafka kafka-console-consumer --topic order-events --bootstrap-server localhost:9092 --from-beginning`
3. Check consumer group: `docker exec -it kafka kafka-consumer-groups --bootstrap-server localhost:9092 --describe --group order-processor`

## Source Code

Complete source code for this example is available at:
- GitHub: [k-entity-framework-examples](https://github.com/cleberMargarida/k-entity-framework/tree/main/samples/DebeziumExample) (when available)

## Additional Resources

- [Debezium Overview](debezium-overview.md)
- [SQL Server Setup](debezium-sqlserver.md)
- [PostgreSQL Setup](debezium-postgresql.md)
- [Outbox Pattern Documentation](../features/outbox.md)
