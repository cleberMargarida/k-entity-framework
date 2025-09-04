# Installation

This guide covers how to install and set up K-Entity-Framework in your .NET project.

## Package Installation

### Via Package Manager Console

```powershell
Install-Package K.EntityFrameworkCore
```

### Via .NET CLI

```bash
dotnet add package K.EntityFrameworkCore
```

### Via PackageReference

Add the following to your `.csproj` file:

```xml
<PackageReference Include="K.EntityFrameworkCore" Version="latest" />
```

## Dependencies

K-Entity-Framework requires the following dependencies (automatically installed):

- **Microsoft.EntityFrameworkCore** (8.0+)
- **Confluent.Kafka** (2.10.1)

## Optional Dependencies

Depending on your needs, you may want to install additional packages:

### For SQL Server Integration
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### For PostgreSQL Integration
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

## Verify Installation

Create a simple test to verify the installation:

```csharp
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<TestMessage>(topic =>
        {
            topic.HasName("test-topic");
            topic.UseSystemTextJson();
            
            topic.HasProducer(producer =>
            {
                producer.HasKey(m => m.Content);
            });
        });
    }
}

public class TestMessage
{
    public string Content { get; set; }
}

// Service configuration test
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TestDbContext>(options => options
    .UseInMemoryDatabase("test-db")
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
    }));

var app = builder.Build();
```

If this compiles without errors, your installation is successful.

## Framework Compatibility

| Framework | Supported |
|-----------|-----------|
| .NET 8.0+ | ✅         |
| .NET 9.0+ | ✅         |

## Next Steps

- [Quick Start Guide](quick-start.md) - Build your first producer and consumer
- [Basic Usage](basic-usage.md) - Learn the fundamentals