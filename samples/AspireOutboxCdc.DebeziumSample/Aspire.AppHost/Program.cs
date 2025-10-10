var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres");

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(pdAdmin => pdAdmin.WithImageTag("9.2.0"))
    .WithPassword(postgresPassword)
    .WithImageTag("16.4")
    .WithArgs("-c", "wal_level=logical");//required for cdc

var kafka = builder.AddKafka("kafka", 56535);

var kafkaConnect = builder.AddContainer("kafka-connect", "quay.io/debezium/connect", "latest")
    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "http")
    .WithEnvironment("BOOTSTRAP_SERVERS", "kafka:9093")
    .WithEnvironment("GROUP_ID", "debezium-cluster")
    .WithEnvironment("CONFIG_STORAGE_TOPIC", "connect-configs")
    .WithEnvironment("OFFSET_STORAGE_TOPIC", "connect-offsets")
    .WithEnvironment("STATUS_STORAGE_TOPIC", "connect-status")
    .WithEnvironment("CONFIG_STORAGE_REPLICATION_FACTOR", "1")
    .WithEnvironment("OFFSET_STORAGE_REPLICATION_FACTOR", "1")
    .WithEnvironment("STATUS_STORAGE_REPLICATION_FACTOR", "1")
    .WaitFor(kafka);

/*
 * POST using Windows Powershell!!!!
 */
builder.AddExecutable("connector-setup", "powershell.exe", ".", "-Command",
    @"$maxRetries = 10; $retryCount = 0; do { try { Invoke-RestMethod -Uri http://localhost:8083/connectors -Method POST -ContentType 'application/json' -Body $env:CONNECTOR_CONFIG; break } catch { $retryCount++; if ($retryCount -lt $maxRetries) { Start-Sleep -Seconds 5 } else { throw } } } while ($retryCount -lt $maxRetries)")
    .WithEnvironment("CONNECTOR_CONFIG", /*lang=json,strict*/ """
    {
      "name": "postgres-outbox-connector",
      "config": {
        "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
        "database.hostname": "postgres",
        "database.port": "5432",
        "database.user": "postgres",
        "database.password": "postgres",
        "database.dbname": "postgres",
        "database.server.name": "pgserver1",
        "table.include.list": "public.outbox_messages",
        "publication.name": "dbz_publication",
        "plugin.name": "pgoutput",
        "topic.prefix": "pgserver1",
        "transforms": "outbox",
        "transforms.outbox.type": "io.debezium.transforms.outbox.EventRouter",
        "transforms.outbox.table.field.event.id": "Id",
        "transforms.outbox.table.field.event.key": "AggregateId",
        "transforms.outbox.table.field.event.payload": "Payload",
        "transforms.outbox.route.by.field": "Topic",
        "transforms.outbox.route.topic.regex": "(.*)",
        "transforms.outbox.route.topic.replacement": "$1",
        "transforms.outbox.table.expand.json.payload": "false",
        "transforms.outbox.table.fields.additional.placement": "Headers:header:__debezium.outbox.headers",
        "key.converter": "org.apache.kafka.connect.storage.StringConverter",
        "value.converter": "org.apache.kafka.connect.converters.ByteArrayConverter",
        "slot.name": "dbz_outbox_slot",
        "tombstones.on.delete": "false",
        "errors.tolerance": "all",
        "errors.log.enable": "true",
        "errors.log.include.messages": "true"
      }
    }
    """)
    .WaitForStart(kafkaConnect)
    .WaitForStart(postgres);

builder.AddProject<Projects.ApiWeb>("apiweb")
    .WithReference(postgres)
    .WithReference(kafka)
    .WaitFor(postgres)
    .WaitFor(kafka);

builder.Build().Run();
