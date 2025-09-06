# Exclusive Connection Examples

[Back to Getting Started](../getting-started/index.md)

> **Note (deprecated):** Exclusive connections are no longer recommended for most scenarios. Prefer a shared consumer connection with per-type group IDs (see Getting Started → Basic Examples). Use `consumer.HasGroupId("your-group")` to scope consumers instead of allocating a dedicated connection.

Exclusive connections allocate a dedicated connection for a consumer. This can be useful when consumers need strict isolation or special connection properties.

## Full service configuration

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PostgreTestContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));
var app = builder.Build();
app.Run();
```

## Topic configuration (exclusive consumer)

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("exclusive-connection-topic");
    topic.HasProducer(p => p.HasKey(m => m.OrderId.ToString()));
    topic.HasConsumer(c => c.HasExclusiveConnection());
});
```

Notes:
- Exclusive connections can be combined with consumer group settings if you need a single dedicated connection per process.
- Verify your Kafka client/cluster supports the desired connection characteristics before enabling exclusivity.


