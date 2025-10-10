# Debezium with .NET Aspire Integration

Deploy Debezium and K-Entity-Framework applications using .NET Aspire for streamlined local development and production deployment.

## What is .NET Aspire?

.NET Aspire is a cloud-ready stack for building observable, production-ready, distributed applications. It provides:

- **Orchestration** - Manage multiple services with a single command
- **Service Discovery** - Automatic connection string management
- **Health Checks** - Built-in health monitoring
- **Observability** - Integrated logging, metrics, and tracing
- **Container Management** - Simplified Docker container orchestration

## Prerequisites

- .NET 8.0 SDK or later
- Docker Desktop
- Visual Studio 2022 17.9+ or VS Code with C# Dev Kit
- .NET Aspire workload

### Install .NET Aspire Workload

```powershell
dotnet workload install aspire
```

## Project Setup

### 1. Create Aspire Application

```powershell
# Create solution
dotnet new sln -n OrderService

# Create Aspire host project
dotnet new aspire-apphost -n OrderService.AppHost
dotnet sln add OrderService.AppHost

# Create service defaults
dotnet new aspire-servicedefaults -n OrderService.ServiceDefaults
dotnet sln add OrderService.ServiceDefaults

# Create API project
dotnet new webapi -n OrderService.Api
dotnet sln add OrderService.Api

# Create consumer project
dotnet new worker -n OrderService.Consumer
dotnet sln add OrderService.Consumer
```

### 2. Add Project References

```powershell
# AppHost references
cd OrderService.AppHost
dotnet add reference ..\OrderService.Api
dotnet add reference ..\OrderService.Consumer

# API references
cd ..\OrderService.Api
dotnet add reference ..\OrderService.ServiceDefaults
dotnet add package K.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Consumer references
cd ..\OrderService.Consumer
dotnet add reference ..\OrderService.ServiceDefaults
dotnet add package K.EntityFrameworkCore
```

## Configure AppHost

### 3. Define Infrastructure in Program.cs

Create/update `OrderService.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "orderdb")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithArgs("-c", "wal_level=logical")
    .WithArgs("-c", "max_replication_slots=4")
    .WithArgs("-c", "max_wal_senders=4")
    .WithDataVolume();

var orderDb = postgres.AddDatabase("orderdb");

// Kafka with KRaft (no ZooKeeper needed)
var kafka = builder.AddKafka("kafka")
    .WithDataVolume()
    .WithKafkaUI(); // Adds Kafka UI at http://localhost:8080

// Kafka Connect with Debezium
var kafkaConnect = builder.AddContainer("kafka-connect", "debezium/connect", "2.5")
    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "http")
    .WithEnvironment("BOOTSTRAP_SERVERS", "kafka:9092")
    .WithEnvironment("GROUP_ID", "debezium-cluster")
    .WithEnvironment("CONFIG_STORAGE_TOPIC", "connect-configs")
    .WithEnvironment("OFFSET_STORAGE_TOPIC", "connect-offsets")
    .WithEnvironment("STATUS_STORAGE_TOPIC", "connect-status")
    .WithEnvironment("CONFIG_STORAGE_REPLICATION_FACTOR", "1")
    .WithEnvironment("OFFSET_STORAGE_REPLICATION_FACTOR", "1")
    .WithEnvironment("STATUS_STORAGE_REPLICATION_FACTOR", "1")
    .WaitFor(kafka);

// API Service
var api = builder.AddProject<Projects.OrderService_Api>("api")
    .WithReference(orderDb)
    .WithReference(kafka)
    .WaitFor(orderDb)
    .WaitFor(kafka);

// Consumer Service
builder.AddProject<Projects.OrderService_Consumer>("consumer")
    .WithReference(orderDb)
    .WithReference(kafka)
    .WaitFor(api)
    .WaitFor(kafkaConnect);

builder.Build().Run();
```

## Configure API Service

### 4. Update API Program.cs

Update `OrderService.Api/Program.cs`:

```csharp
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Events;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, etc.)
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<OrderDbContext>("orderdb");

// Configure Kafka with Aspire connection
builder.Services.AddKafka(options =>
{
    options.ConfigureClient(client =>
    {
        // Aspire automatically provides the connection string
        client.BootstrapServers = builder.Configuration.GetConnectionString("kafka")
            ?? "localhost:9092";
    });

    options.ConfigureProducer<OrderCreated>(producer =>
    {
        producer.Acks = Confluent.Kafka.Acks.All;
        producer.EnableIdempotence = true;
    });
});

var app = builder.Build();

// Map Aspire health checks
app.MapDefaultEndpoints();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
    
    // Enable CDC if not already enabled (requires sysadmin on SQL Server)
    // For PostgreSQL, this is done via SQL scripts
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

### 5. Configure DbContext

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

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.OwnsMany(e => e.Items, items =>
            {
                items.Property(i => i.Price).HasPrecision(18, 2);
            });
        });

        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-events");
            topic.HasProducer(producer =>
            {
                producer.HasKey(order => order.OrderId.ToString());
                producer.HasOutbox();
            });
        });
    }
}
```

## Configure Consumer Service

### 6. Update Consumer Program.cs

Update `OrderService.Consumer/Worker.cs`:

```csharp
using K.EntityFrameworkCore.Extensions;
using OrderService.Api.Data;
using OrderService.Api.Events;

namespace OrderService.Consumer;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order consumer starting...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await foreach (var order in dbContext.OrderCreated.ConsumeAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation(
                    "Processing order {OrderId} for customer {CustomerId}, Total: ${Total}",
                    order.OrderId, order.CustomerId, order.TotalAmount);

                // Process order (send email, update inventory, etc.)
                await Task.Delay(100, stoppingToken); // Simulate processing

                _logger.LogInformation("Order {OrderId} processed successfully", order.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId}", order.OrderId);
            }
        }
    }
}
```

Update `OrderService.Consumer/Program.cs`:

```csharp
using K.EntityFrameworkCore.Extensions;
using OrderService.Api.Data;
using OrderService.Api.Events;
using OrderService.Consumer;

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<OrderDbContext>("orderdb");

// Configure Kafka consumer
builder.Services.AddKafka(options =>
{
    options.ConfigureClient(client =>
    {
        client.BootstrapServers = builder.Configuration.GetConnectionString("kafka")
            ?? "localhost:9092";
    });

    options.ConfigureConsumer<OrderCreated>(consumer =>
    {
        consumer.GroupId = "order-processor";
        consumer.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
        consumer.EnableAutoCommit = false;
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

## Database Setup

### 7. Enable PostgreSQL CDC

Create `setup-postgres-cdc.sql`:

```sql
-- Enable logical replication (already configured in docker args)
-- Create publication for outbox table
CREATE PUBLICATION dbz_publication FOR TABLE public.outbox_messages;

-- Verify publication
SELECT * FROM pg_publication;
```

Apply after first run:

```powershell
docker exec -i $(docker ps --filter "name=postgres" --format "{{.ID}}") psql -U postgres -d orderdb < setup-postgres-cdc.sql
```

### 8. Deploy Debezium Connector

Create `connector-setup.ps1`:

```powershell
# Wait for Kafka Connect to be ready
Write-Host "Waiting for Kafka Connect..."
do {
    Start-Sleep -Seconds 2
    $status = try { Invoke-RestMethod -Uri "http://localhost:8083/" -ErrorAction SilentlyContinue } catch { $null }
} while (-not $status)

Write-Host "Kafka Connect is ready. Deploying connector..."

# Deploy connector
$connector = @{
    name = "postgres-outbox-connector"
    config = @{
        "connector.class" = "io.debezium.connector.postgresql.PostgresConnector"
        "database.hostname" = "postgres"
        "database.port" = "5432"
        "database.user" = "postgres"
        "database.password" = "postgres"
        "database.dbname" = "orderdb"
        "database.server.name" = "pgserver"
        "table.include.list" = "public.outbox_messages"
        "publication.name" = "dbz_publication"
        "plugin.name" = "pgoutput"
        "slot.name" = "debezium_slot"
        
        "transforms" = "outbox"
        "transforms.outbox.type" = "io.debezium.transforms.outbox.EventRouter"
        "transforms.outbox.table.field.event.id" = "Id"
        "transforms.outbox.table.field.event.key" = "AggregateId"
        "transforms.outbox.table.field.event.type" = "Type"
        "transforms.outbox.table.field.payload" = "Payload"
        "transforms.outbox.route.by.field" = "Topic"
        
        "key.converter" = "org.apache.kafka.connect.storage.StringConverter"
        "value.converter" = "org.apache.kafka.connect.json.JsonConverter"
        "value.converter.schemas.enable" = "false"
    }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:8083/connectors" `
    -Method Post `
    -ContentType "application/json" `
    -Body $connector

Write-Host "Connector deployed successfully!"
```

## Running the Application

### 9. Start Everything

```powershell
# Run Aspire AppHost (starts all services)
cd OrderService.AppHost
dotnet run
```

This will:
1. Start PostgreSQL with logical replication enabled
2. Start Kafka with KRaft
3. Start Kafka Connect with Debezium
4. Start Kafka UI at http://localhost:8080
5. Start the API service
6. Start the consumer service
7. Open the Aspire dashboard at http://localhost:15888

### 10. Setup Connector

In another terminal:

```powershell
# Run setup script
.\connector-setup.ps1

# Or manually deploy
curl -X POST http://localhost:8083/connectors `
  -H "Content-Type: application/json" `
  -d @connector.json
```

### 11. Create Test Order

```powershell
$apiPort = # Check Aspire dashboard for actual port
curl -X POST http://localhost:$apiPort/api/orders `
  -H "Content-Type: application/json" `
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "items": [
      { "productId": "PROD-001", "quantity": 2, "price": 29.99 }
    ]
  }'
```

## Monitoring with Aspire Dashboard

The Aspire dashboard (http://localhost:15888) provides:

- **Logs** - View logs from all services in one place
- **Traces** - Distributed tracing across services
- **Metrics** - Real-time metrics and performance data
- **Resources** - Container status and resource usage
- **Environment** - Configuration and connection strings

### View Order Flow

1. Open Aspire dashboard
2. Go to **Traces** tab
3. Create an order via API
4. See the complete trace:
   - HTTP request to API
   - Database insert
   - Outbox message created
   - Event published to Kafka
   - Consumer receives and processes

## Production Deployment

### Azure Deployment

Aspire can deploy to Azure Container Apps:

```powershell
# Install Azure Developer CLI
winget install Microsoft.Azd

# Login
azd auth login

# Initialize
azd init

# Deploy
azd up
```

### Kubernetes Deployment

Generate Kubernetes manifests:

```powershell
# Add manifest generation
dotnet add package Aspire.Hosting.Azure.AppContainers

# Generate manifests
dotnet run --project OrderService.AppHost -- --output-path ./manifests
```

## Best Practices

### 1. Use Health Checks

Aspire automatically adds health checks, but you can customize:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("orderdb")!)
    .AddKafka(new ProducerConfig
    {
        BootstrapServers = builder.Configuration.GetConnectionString("kafka")
    });
```

### 2. Configure Resource Limits

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_SHARED_BUFFERS", "256MB")
    .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "100");
```

### 3. Add Persistent Volumes

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("postgres-data")
    .WithDataBindMount("./data/postgres"); // For local development
```

### 4. Configure Service Dependencies

```csharp
var api = builder.AddProject<Projects.OrderService_Api>("api")
    .WaitFor(orderDb)      // Wait for DB to be ready
    .WaitFor(kafka)        // Wait for Kafka to be ready
    .WaitForCompletion(migrations); // Wait for migrations
```

## Troubleshooting

### Services Not Starting

Check Aspire dashboard logs for errors. Common issues:
- Port conflicts (change in AppHost)
- Docker not running
- Missing database migrations

### Connector Not Deploying

```powershell
# Check Kafka Connect logs
docker logs $(docker ps --filter "name=kafka-connect" --format "{{.ID}}")

# Verify Kafka Connect is ready
curl http://localhost:8083/
```

### No Events in Kafka

1. Check connector status: `curl http://localhost:8083/connectors/postgres-outbox-connector/status`
2. Verify CDC publication: `SELECT * FROM pg_publication_tables;`
3. Check outbox table: `SELECT * FROM outbox_messages;`

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire GitHub Repository](https://github.com/dotnet/aspire)
- [Debezium Overview](debezium-overview.md)
- [PostgreSQL Setup](debezium-postgresql.md)
- [Complete Example](debezium-example.md)
