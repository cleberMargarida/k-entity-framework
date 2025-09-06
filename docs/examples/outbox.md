# Outbox Pattern Examples

[Back to Getting Started](../getting-started/index.md)

## Full service configuration

The examples below assume an ASP.NET Core application. Register the DbContext and Kafka client before configuring topics.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PostgreTestContext>(opts => opts
    
    // DbContext with PostgreSQL (replace connection string accordingly)
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    
    // Register DbContext and configure Kafka extensibility on it
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 50;
});

var app = builder.Build();
app.Run();
```

## Background-only outbox

Use this strategy when durability is more important than immediate delivery. Messages are stored in the outbox table and a worker publishes them asynchronously.

Configuration (topic-level):

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("outbox-test-topic");
    topic.HasProducer(producer =>
    {
        producer.HasKey(m => m.OrderId.ToString());
        producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
    });
});
```

Producer usage (application code):

```csharp
// Enqueue the message and persist the DbContext change. The message will
// be recorded in the outbox table and published by the background worker.
dbContext.OrderEvents.Produce(new OrderCreated { OrderId = 42, CustomerId = "OutboxTest" });
await dbContext.SaveChangesAsync();
```

## Immediate with fallback

This strategy first adds the message to the outbox for potential retries by the worker, then immediately attempts to publish it synchronously during `SaveChanges`. If publishing succeeds, the message is removed from the outbox, eliminating the need for worker processing.

Topic configuration:

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("immediate-fallback-topic");
    topic.HasProducer(p => p.HasKey(m => m.OrderId.ToString())
        .HasOutbox(o => o.UseImmediateWithFallback()));
});
```

Producer usage is identical from the app perspective — call SaveChanges and the library performs the immediate publish attempt with fallback.

## Batch publishing notes

When producing many messages in one DbContext SaveChanges operation, the library will handle batching according to the worker settings and producer configuration. Tune worker batch sizes and polling intervals in `AddOutboxKafkaWorker` options.

 