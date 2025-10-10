# Debezium Integration

![alt text](../images/debezium-banner.png)
Use Debezium with K-Entity-Framework's outbox pattern for low-latency, real-time event streaming from your database.

## Overview

Debezium captures database changes directly from transaction logs and publishes them to Kafka. This provides an alternative to the built-in polling worker with millisecond latency.

## Setup

### 1. Docker Infrastructure

Create `docker-compose.yml`:

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

Start with: `docker-compose up -d`

### 2. Enable CDC on SQL Server

```sql
-- Enable CDC on database
USE MyDatabase;
EXEC sys.sp_cdc_enable_db;

-- Enable CDC on outbox table
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'outbox_messages',
    @supports_net_changes = 1;
```

### 3. Create Debezium Connector

Create `connector.json`:

```json
{
  "name": "outbox-connector",
  "config": {
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "database.hostname": "localhost",
    "database.port": "1433",
    "database.user": "sa",
    "database.password": "YourPassword",
    "database.dbname": "MyDatabase",
    "database.server.name": "server1",
    "table.include.list": "dbo.outbox_messages",
    "transforms": "outbox",
    "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
    "transforms.outbox.table.field.event.id": "Id",
    "transforms.outbox.table.field.event.key": "AggregateId",
    "transforms.outbox.table.field.event.type": "Type",
    "transforms.outbox.table.field.payload": "Payload",
    "transforms.outbox.route.by.field": "Topic"
  }
}
```

**Important:** The `transforms.outbox.route.by.field` is now set to `"Topic"` instead of `"Type"`. This allows proper routing when using custom topic names configured via `topic.HasName("custom-topic-name")`.

Deploy: `curl -X POST http://localhost:8083/connectors -H "Content-Type: application/json" -d @connector.json`

## Application Configuration

### Configure Topics

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

### Topic Routing Behavior

The outbox table includes a required `Topic` column that stores the topic name for each message:

- **Default behavior**: If no custom topic name is configured, the `Topic` field will contain the default topic name (the message type's full name)
- **Custom topic names**: When using `topic.HasName("custom-name")`, the `Topic` field will contain `"custom-name"`
- **Always populated**: The `Topic` field is never null, ensuring consistent Debezium routing behavior
- **Debezium routing**: The EventRouter uses the `Topic` field to determine the correct Kafka topic for each message

This approach ensures that Debezium can always route messages correctly, whether you use default or custom topic names.

### Produce Messages

```csharp
// Messages are stored in outbox and published by Debezium
await dbContext.OrderCreated.ProduceAsync(new OrderCreated { OrderId = 123 });
await dbContext.SaveChangesAsync();
```

## When to Use Debezium

- **High-throughput applications** requiring millisecond latency
- **Real-time event processing** needs
- **Large-scale deployments** where polling overhead is significant

For simpler setups, use the built-in [OutboxPollingWorker](../features/outbox.md) instead.
