# Inbox Deduplication Examples

[Back to Getting Started](../getting-started/index.md)

Deduplication prevents a consumer from processing the same logical message more than once within a configured window. Below is a documentation-friendly example with full wiring.

## Full service configuration

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PostgreTestContext>(opts => opts
    
    // DbContext with PostgreSQL (replace connection string accordingly)
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    
    // Register DbContext and configure Kafka extensibility on it
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("Kafka")));

app.Run();
```

## Topic and inbox configuration

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasName("deduplication-topic");
    topic.HasProducer(p => p.HasKey(m => m.OrderId.ToString()));
    topic.HasConsumer(c =>
    {
        c.HasInbox(inbox =>
        {
            // Choose the properties that identify duplicates
            inbox.HasDeduplicateProperties(m => new { m.OrderId });

            // Keep deduplication state for a window to avoid reprocessing
            inbox.UseDeduplicationTimeWindow(TimeSpan.FromMinutes(5));
        });
    });
});
```

> [!NOTE]
> When duplicate messages are received within the configured window, the library will skip processing according to the inbox strategy. 
> The consumer still receives the Kafka record; the inbox middleware decides whether to execute the message handler.
