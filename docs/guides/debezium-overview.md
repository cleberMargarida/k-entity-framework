# Debezium Integration Overview

![alt text](../images/debezium-banner.png)

Use Debezium with K-Entity-Framework's outbox pattern for low-latency, real-time event streaming from your database.

## What is Debezium?

Debezium is an open-source distributed platform for change data capture (CDC). It captures row-level changes in your database and streams them to Kafka topics in real-time. When integrated with K-Entity-Framework's outbox pattern, Debezium provides:

- **Millisecond latency** - Changes are captured directly from transaction logs
- **Guaranteed delivery** - No messages are lost, even during failures
- **Exactly-once semantics** - Messages are published exactly once per database transaction
- **Zero application overhead** - No polling workers needed

## How It Works

1. **Application writes** to the outbox table within a transaction
2. **Debezium captures** the database change from the transaction log
3. **EventRouter SMT** transforms the outbox record into a proper Kafka message
4. **Message is published** to the configured Kafka topic automatically

## Architecture

```
┌─────────────┐
│ Application │
│  + EF Core  │
└──────┬──────┘
       │ Insert into outbox_messages
       ↓
┌─────────────┐
│  Database   │
│ (SQL Server,│
│ PostgreSQL) │
└──────┬──────┘
       │ CDC Stream
       ↓
┌─────────────┐
│  Debezium   │
│  Connector  │
└──────┬──────┘
       │ Transformed Event
       ↓
┌─────────────┐
│    Kafka    │
└─────────────┘
```

## When to Use Debezium

### ✅ Use Debezium when:
- You need **sub-second latency** for event publishing
- You have **high-throughput** requirements (thousands of events/second)
- You're building **event-driven architectures** requiring real-time processing
- You want to **eliminate polling overhead** from your application
- You need **operational visibility** into database changes

### ⚠️ Use Polling Worker when:
- You have a **simple setup** with low event volume
- **Ease of deployment** is more important than latency
- You don't want to manage CDC infrastructure
- Events can be delayed by seconds without impact

For polling worker setup, see the [OutboxPollingWorker documentation](../features/outbox.md).

## Next Steps

- [SQL Server Setup](debezium-sqlserver.md) - Configure CDC for SQL Server
- [PostgreSQL Setup](debezium-postgresql.md) - Configure CDC for PostgreSQL
- [Complete Example](debezium-example.md) - Full working implementation
- [Aspire Integration](debezium-aspire.md) - Deploy with .NET Aspire

## Common Configuration

### Topic Routing Behavior

The outbox table includes a required `Topic` column that stores the topic name for each message:

- **Default behavior**: If no custom topic name is configured, the `Topic` field will contain the default topic name (the message type's full name)
- **Custom topic names**: When using `topic.HasName("custom-name")`, the `Topic` field will contain `"custom-name"`
- **Always populated**: The `Topic` field is never null, ensuring consistent Debezium routing behavior
- **Debezium routing**: The EventRouter uses the `Topic` field to determine the correct Kafka topic for each message

### Configure Topics in Your DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasName("orders-topic");  // Custom topic name
        topic.HasProducer(producer =>
        {
            producer.HasKey(order => order.OrderId);
            producer.HasOutbox();
        });
    });
}
```

### Produce Messages

```csharp
// Messages are stored in outbox and published by Debezium
await dbContext.OrderCreated.ProduceAsync(new OrderCreated { OrderId = 123 });
await dbContext.SaveChangesAsync();
```

## Resources

- [Debezium Documentation](https://debezium.io/documentation/)
- [Outbox Event Router](https://debezium.io/documentation/reference/transformations/outbox-event-router.html)
- [K-Entity-Framework Outbox Pattern](../features/outbox.md)
