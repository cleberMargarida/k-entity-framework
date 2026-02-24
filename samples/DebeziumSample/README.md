# Debezium Sample

This sample demonstrates the transactional outbox pattern with Debezium and K-Entity-Framework.
No external orchestration is required; a `docker-compose.yml` spins up PostgreSQL, Kafka (KRaft), and Kafka Connect with a custom SMT. The .NET console app writes an order and an outbox event to the database, then consumes the event via the EF Core Kafka integration.

## Quick start

1. Build the custom Kafka Connect image:
   ```powershell
   docker build -t k-entity-framework/kafka-connect-smt ../../src/kafka-connect-smt
   ```
2. Start infrastructure:
   ```powershell
   cd samples\DebeziumSample
   docker compose up -d
   ```
   Wait for the `connector-setup` service to finish registering the Debezium connector (check `docker compose logs connector-setup`).
3. Run the console application:
   ```powershell
   cd DebeziumSample
   dotnet run
   ```
4. You should see output like:
   ```
   Order placed: <guid> at <timestamp>
   ```

### Configuration
- `appsettings.json` contains default connection strings pointing to `localhost:5432` (Postgres) and `localhost:9092` (Kafka). Adjust as needed.
- The Debezium connector configuration is in `connector-config.json` and is posted by the `connector-setup` service.

### Cleanup
```powershell
# stop the app (Ctrl+C)
docker compose down
```

This sample is intentionally lightweight and does not rely on .NET Aspire; it should be easy to plug into any environment.
