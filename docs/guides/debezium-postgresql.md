# Debezium Integration with PostgreSQL

This guide covers setting up Debezium with PostgreSQL and K-Entity-Framework's outbox pattern.

## Prerequisites

- PostgreSQL 10 or later
- Logical replication enabled
- Appropriate database permissions (REPLICATION or superuser)

## Step 1: Docker Infrastructure

Create a `docker-compose.yml` file:

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    ports: 
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: mydb
    command:
      - "postgres"
      - "-c"
      - "wal_level=logical"  # Required for CDC
      - "-c"
      - "max_replication_slots=4"
      - "-c"
      - "max_wal_senders=4"
    volumes:
      - postgres_data:/var/lib/postgresql/data

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
    depends_on: [kafka, postgres]
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

volumes:
  postgres_data:
```

Start the services:

```powershell
docker-compose up -d
```

## Step 2: Configure PostgreSQL

### Verify Logical Replication is Enabled

```sql
-- Connect to PostgreSQL
-- psql -U postgres -d mydb

-- Check wal_level setting
SHOW wal_level;
-- Should return: logical

-- Check replication slots configuration
SHOW max_replication_slots;
SHOW max_wal_senders;
```

### Create Replication User (Optional)

For production, create a dedicated replication user:

```sql
-- Create replication user
CREATE USER debezium WITH REPLICATION PASSWORD 'debezium_password';

-- Grant necessary permissions
GRANT CONNECT ON DATABASE mydb TO debezium;
GRANT USAGE ON SCHEMA public TO debezium;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO debezium;

-- Grant permissions on outbox table
GRANT SELECT ON public.outbox_messages TO debezium;
```

### Configure Publication

PostgreSQL uses publications for logical replication:

```sql
-- Create publication for outbox table
CREATE PUBLICATION dbz_publication FOR TABLE public.outbox_messages;

-- Verify publication
SELECT * FROM pg_publication;
SELECT * FROM pg_publication_tables;
```

## Step 3: Create Debezium Connector

Create a `postgresql-connector.json` file:

```json
{
  "name": "postgres-outbox-connector",
  "config": {
    "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
    "database.hostname": "postgres",
    "database.port": "5432",
    "database.user": "postgres",
    "database.password": "postgres",
    "database.dbname": "mydb",
    "database.server.name": "pgserver1",
    "table.include.list": "public.outbox_messages",
    "publication.name": "dbz_publication",
    "plugin.name": "pgoutput",
    
    "transforms": "outbox",
    "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
    "transforms.outbox.table.field.event.id": "Id",
    "transforms.outbox.table.field.event.key": "AggregateId",
    "transforms.outbox.table.field.event.type": "Type",
    "transforms.outbox.table.field.payload": "Payload",
    "transforms.outbox.route.by.field": "Topic",
    
    "key.converter": "org.apache.kafka.connect.storage.StringConverter",
    "value.converter": "org.apache.kafka.connect.json.JsonConverter",
    "value.converter.schemas.enable": "false",
    
    "slot.name": "dbz_outbox_slot",
    "tombstones.on.delete": "false"
  }
}
```

Deploy the connector:

```powershell
curl -X POST http://localhost:8083/connectors `
  -H "Content-Type: application/json" `
  -d '@postgresql-connector.json'
```

### Verify Connector Status

```powershell
# Check connector status
curl http://localhost:8083/connectors/postgres-outbox-connector/status

# View all connectors
curl http://localhost:8083/connectors
```

## Configuration Options

### Connection Settings

| Property | Description | Default |
|----------|-------------|---------|
| `database.hostname` | PostgreSQL host | - |
| `database.port` | PostgreSQL port | `5432` |
| `database.user` | Database user | - |
| `database.password` | User password | - |
| `database.dbname` | Database name | - |
| `database.server.name` | Logical server name | - |

### Logical Decoding Plugin

PostgreSQL supports multiple plugins:

| Plugin | Description | Availability |
|--------|-------------|--------------|
| `pgoutput` | Built-in, recommended | PostgreSQL 10+ |
| `wal2json` | JSON output format | Requires extension |
| `decoderbufs` | Protocol buffers | Requires extension |

For most cases, use `pgoutput`:

```json
{
  "plugin.name": "pgoutput"
}
```

### Replication Slot

```json
{
  "slot.name": "dbz_outbox_slot",
  "slot.drop.on.stop": "false"
}
```

⚠️ **Important**: Set `slot.drop.on.stop` to `false` in production to prevent data loss when the connector restarts.

## Monitoring

### View Replication Slots

```sql
-- View active replication slots
SELECT * FROM pg_replication_slots;

-- Check replication lag
SELECT 
    slot_name,
    pg_size_pretty(pg_wal_lsn_diff(pg_current_wal_lsn(), restart_lsn)) as replication_lag
FROM pg_replication_slots;
```

### View Publications

```sql
-- List publications
SELECT * FROM pg_publication;

-- List tables in publication
SELECT * FROM pg_publication_tables WHERE pubname = 'dbz_publication';
```

### Connector Logs

```powershell
# View Kafka Connect logs
docker logs kafka-connect --follow

# Filter connector-specific logs
docker logs kafka-connect 2>&1 | Select-String "postgres-outbox-connector"
```

## Application Configuration

### Configure Topics in Your DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasName("orders-topic");
        topic.HasProducer(producer =>
        {
            producer.HasKey(order => order.OrderId);
            producer.HasOutbox();
        });
    });
}
```

### Configure PostgreSQL Provider

```csharp
services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### Produce Messages

```csharp
// Messages are stored in outbox and published by Debezium
await dbContext.OrderCreated.ProduceAsync(new OrderCreated 
{ 
    OrderId = 123,
    CustomerId = 456,
    Total = 99.99m
});
await dbContext.SaveChangesAsync();
```

## Troubleshooting

### Logical Replication Not Enabled

If you get an error about WAL level:

```sql
-- Check current setting
SHOW wal_level;

-- If not 'logical', edit postgresql.conf:
-- wal_level = logical
-- max_replication_slots = 4
-- max_wal_senders = 4

-- Then restart PostgreSQL
```

In Docker, ensure the command-line parameters are set:

```yaml
command:
  - "postgres"
  - "-c"
  - "wal_level=logical"
```

### Publication Not Found

```sql
-- Create publication if missing
CREATE PUBLICATION dbz_publication FOR TABLE public.outbox_messages;

-- Or for all tables in schema
CREATE PUBLICATION dbz_publication FOR ALL TABLES;
```

### Replication Slot Issues

If the connector can't create a slot:

```sql
-- Check current slots
SELECT * FROM pg_replication_slots;

-- Drop unused slot
SELECT pg_drop_replication_slot('dbz_outbox_slot');

-- Ensure enough slots available
ALTER SYSTEM SET max_replication_slots = 4;
SELECT pg_reload_conf();
```

### Connection Issues from Docker

When running PostgreSQL outside Docker:

- Use `host.docker.internal` instead of `localhost`
- Ensure `pg_hba.conf` allows connections from Docker network
- Check firewall allows port 5432

```conf
# pg_hba.conf
host    all    all    172.17.0.0/16    md5
```

## Performance Considerations

### WAL Disk Usage

Monitor WAL disk usage to prevent disk space issues:

```sql
-- Check WAL usage
SELECT 
    slot_name,
    pg_size_pretty(pg_wal_lsn_diff(pg_current_wal_lsn(), restart_lsn)) as retained_wal
FROM pg_replication_slots;

-- Check WAL directory size
SELECT pg_size_pretty(sum(size)) 
FROM pg_ls_waldir();
```

### Connector Performance

For high-throughput scenarios:

```json
{
  "max.batch.size": "2048",
  "max.queue.size": "8192",
  "poll.interval.ms": "100",
  "heartbeat.interval.ms": "1000"
}
```

### Table Filtering

Only capture the outbox table to minimize overhead:

```json
{
  "table.include.list": "public.outbox_messages",
  "publication.name": "dbz_publication"
}
```

## Security Best Practices

### Use Dedicated Replication User

```sql
CREATE USER debezium WITH REPLICATION PASSWORD 'strong_password';
GRANT SELECT ON public.outbox_messages TO debezium;
```

### SSL/TLS Encryption

```json
{
  "database.sslmode": "require",
  "database.sslrootcert": "/path/to/ca.crt"
}
```

### Restrict pg_hba.conf

```conf
# Only allow replication from specific hosts
host    replication    debezium    172.17.0.0/16    md5
```

## Next Steps

- [Complete Example](debezium-example.md) - Full working implementation
- [Aspire Integration](debezium-aspire.md) - Deploy with .NET Aspire
- [Overview](debezium-overview.md) - Back to Debezium overview
