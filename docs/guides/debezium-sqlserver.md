# Debezium Integration with SQL Server

This guide covers setting up Debezium with SQL Server and K-Entity-Framework's outbox pattern.

> **Prerequisite:** Read the [Debezium Overview](debezium-overview.md) to understand the custom `HeaderJsonExpander` SMT that is required for correct header propagation.

## Prerequisites

- SQL Server 2016 or later (Express, Standard, or Enterprise)
- SQL Server Agent running (required for CDC cleanup jobs)
- Appropriate database permissions (db_owner or specific CDC permissions)

## Step 1: Docker Infrastructure

Create a `docker-compose.yml` file:

```yaml
services:
  kafka:
    image: apache/kafka:latest
    ports: 
      - "9092:9092"
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://0.0.0.0:9092,PLAINTEXT_INTERNAL://0.0.0.0:9093,CONTROLLER://0.0.0.0:9094
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092,PLAINTEXT_INTERNAL://kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9094
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT_INTERNAL
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1

  # Use the custom image that bundles the HeaderJsonExpander SMT.
  # See the Debezium Overview for build instructions.
  kafka-connect:
    image: clebermargarida/kafka-connect-smt:latest
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

Start the services:

```powershell
docker-compose up -d
```

Verify Kafka Connect is running:

```powershell
curl http://localhost:8083/
```

## Step 2: Enable CDC on SQL Server

### Enable CDC on Database

```sql
-- Check if CDC is already enabled
SELECT name, is_cdc_enabled 
FROM sys.databases 
WHERE name = 'MyDatabase';

-- Enable CDC on database
USE MyDatabase;
GO

EXEC sys.sp_cdc_enable_db;
GO

-- Verify CDC is enabled
SELECT name, is_cdc_enabled 
FROM sys.databases 
WHERE name = 'MyDatabase';
```

### Enable CDC on Outbox Table

```sql
-- Enable CDC on the outbox_messages table
USE MyDatabase;
GO

EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'outbox_messages',
    @role_name = NULL,  -- NULL means no role required to query CDC data
    @supports_net_changes = 1;
GO

-- Verify CDC is enabled on table
SELECT name, is_tracked_by_cdc 
FROM sys.tables 
WHERE name = 'outbox_messages';
```

### Verify CDC Capture Job

CDC requires SQL Server Agent to be running:

```sql
-- Check if SQL Server Agent is running
EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';

-- View CDC jobs
SELECT * FROM msdb.dbo.cdc_jobs;
```

## Step 3: Create Debezium Connector

Create a `sqlserver-connector.json` file:

```json
{
  "name": "outbox-connector",
  "config": {
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "database.hostname": "host.docker.internal",
    "database.port": "1433",
    "database.user": "sa",
    "database.password": "YourStrong@Password",
    "database.dbname": "MyDatabase",
    "database.server.name": "server1",
    "table.include.list": "dbo.outbox_messages",
    "database.encrypt": "false",
    "database.trustServerCertificate": "true",
    
    "transforms": "outbox,expandHeaders",
    "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
    "transforms.outbox.table.field.event.id": "Id",
    "transforms.outbox.table.field.event.key": "AggregateId",
    "transforms.outbox.table.field.event.payload": "Payload",
    "transforms.outbox.route.by.field": "Topic",
    "transforms.outbox.table.fields.additional.placement": "Headers:header:__debezium.outbox.headers",
    "transforms.expandHeaders.type": "k.entityframework.kafka.connect.transforms.HeaderJsonExpander",

    "key.converter": "org.apache.kafka.connect.storage.StringConverter",
    "value.converter": "org.apache.kafka.connect.converters.ByteArrayConverter"
  }
}
```

Deploy the connector:

```powershell
curl -X POST http://localhost:8083/connectors `
  -H "Content-Type: application/json" `
  -d '@sqlserver-connector.json'
```

### Verify Connector Status

```powershell
# Check connector status
curl http://localhost:8083/connectors/outbox-connector/status

# View all connectors
curl http://localhost:8083/connectors
```

## Configuration Options

### Connection Settings

| Property | Description | Default |
|----------|-------------|---------|
| `database.hostname` | SQL Server host | - |
| `database.port` | SQL Server port | `1433` |
| `database.user` | Database user | - |
| `database.password` | User password | - |
| `database.dbname` | Database name | - |
| `database.encrypt` | Enable TLS encryption | `true` |
| `database.trustServerCertificate` | Trust self-signed certs | `false` |

### Table Filtering

```json
{
  "table.include.list": "dbo.outbox_messages,schema2.another_table",
  "table.exclude.list": "dbo.audit_log"
}
```

### EventRouter Transform

The EventRouter transform maps outbox table columns to Kafka message fields:

| Property | Outbox Column | Description |
|----------|---------------|-------------|
| `table.field.event.id` | `Id` | Unique event identifier |
| `table.field.event.key` | `AggregateId` | Message key for partitioning |
| `table.field.event.type` | `Type` | Event type (message type name) |
| `table.field.payload` | `Payload` | JSON message payload |
| `route.by.field` | `Topic` | Target Kafka topic |

## Monitoring

### View CDC Table Data

```sql
-- View captured changes
SELECT * FROM cdc.dbo_outbox_messages_CT;

-- Check CDC LSN positions
SELECT * FROM cdc.lsn_time_mapping;
```

### Connector Logs

```powershell
# View Kafka Connect logs
docker logs kafka-connect --follow

# View connector-specific logs
docker logs kafka-connect 2>&1 | Select-String "outbox-connector"
```

## Troubleshooting

### SQL Server Agent Not Running

CDC requires SQL Server Agent:

```sql
-- Start SQL Server Agent (requires sysadmin)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

-- On Windows, start via services
-- Or use SQL Server Configuration Manager
```

### Connection Issues from Docker

When connecting from Docker to SQL Server on the host machine:

- Use `host.docker.internal` instead of `localhost`
- Ensure SQL Server is configured to accept TCP/IP connections
- Check Windows Firewall allows port 1433

```sql
-- Enable TCP/IP protocol in SQL Server Configuration Manager
-- Or via PowerShell:
# Get-Service | Where-Object {$_.Name -like "*SQL*"}
```

### CDC Not Capturing Changes

```sql
-- Check CDC capture job status
SELECT * FROM sys.dm_cdc_errors;

-- Manually run capture job
EXEC sys.sp_cdc_scan;
```

### Connector Fails to Start

Check the connector status for error messages:

```powershell
curl http://localhost:8083/connectors/outbox-connector/status | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

Common issues:
- Incorrect credentials
- CDC not enabled on table
- Network connectivity problems
- SQL Server Agent not running

## Performance Considerations

### CDC Retention

CDC data is cleaned up automatically, but you can configure retention:

```sql
-- Set retention to 3 days (4320 minutes)
EXEC sys.sp_cdc_change_job 
    @job_type = N'cleanup',
    @retention = 4320;
```

### Connector Performance

For high-throughput scenarios, adjust connector settings:

```json
{
  "max.batch.size": "2048",
  "max.queue.size": "8192",
  "poll.interval.ms": "500"
}
```

## Next Steps

- [Complete Example](debezium-example.md) - Full working implementation
- [Runnable Sample](https://github.com/cleberMargarida/k-entity-framework/blob/master/samples/DebeziumSample/README.md) - Standalone `docker-compose` + .NET console app
- [Aspire Integration](debezium-aspire.md) - Optional: deploy with .NET Aspire for richer local orchestration
- [Overview](debezium-overview.md) - Back to Debezium overview
