# Forget / Fire-and-Forget Examples

[Back to Getting Started](../getting-started/index.md)

This document shows how to configure fire-and-forget (forget) strategies for producers. The examples are adapted from test scenarios but include complete application wiring and configuration.

## Full service configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register DbContext and apply Kafka extensibility on the DbContext options
builder.Services.AddDbContext<PostgreTestContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

// No outbox worker required for pure fire-and-forget, but you may still add it
// if you also use outbox strategies elsewhere.
// builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();

var app = builder.Build();
app.Run();
```

## Fire-and-forget (fire-and-forget semantics)

Use this strategy when you want the producer to return immediately and not wait for confirmation from Kafka. It's appropriate for low-criticality events where best-effort delivery is acceptable.

Topic configuration example:

```csharp
modelBuilder.Topic<OrderCreated>(t =>
{
    t.HasName("fire-forget-topic");
    t.HasProducer(p =>
    {
        p.HasKey(m => m.OrderId.ToString());
        p.HasForget(f => f.UseFireForget());
    });
});
```

Producer usage:

```csharp
dbContext.OrderEvents.Produce(new OrderCreated { OrderId = 2400, CustomerId = "FireForgetTest" });
await dbContext.SaveChangesAsync(); // returns immediately; library will not await Kafka confirmation
```

## Await-forget (timed wait)

An await-forget strategy can be used to wait for a short confirmation period; if not confirmed, the operation can be treated as fire-and-forget or retried.

Configure with a timeout:

```csharp
t.HasProducer(p => p.HasForget(f => f.UseAwaitForget(TimeSpan.FromSeconds(10))));
```

Note: some test cases in the repository are skipped because forget/backpressure behavior is still evolving — treat the test code as guidance and use the configuration snippets above for application-level docs.


