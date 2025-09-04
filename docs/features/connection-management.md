# Connection Management

K-Entity-Framework uses a single, shared producer and a single, shared consumer connection by default. This is a best practice that avoids resource exhaustion. However, you can configure exclusive connections for consumers that need to be isolated.

## Global Connections

By default, there is one producer and one consumer for the entire application. You can configure global settings for these connections in your `DbContext` configuration.

```csharp
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        // Global consumer settings
        client.Consumer.GroupId = "my-global-group";
    }));
```

## Exclusive Consumer Connections

You can configure a message type to use its own dedicated consumer connection. This is useful for high-priority messages or for isolating consumers from each other.

When you configure an exclusive connection, a new `KafkaConsumerPollService` is created for that specific message type, with its own underlying `IConsumer` instance.

```csharp
modelBuilder.Topic<CriticalAlert>(topic =>
{
    topic.HasName("critical-alerts");
    
    topic.HasConsumer(consumer =>
    {
        consumer.HasExclusiveConnection(connection =>
        {
            connection.GroupId = "critical-alerts-processor";
            connection.AutoOffsetReset = AutoOffsetReset.Earliest;
        });
    });
});
```