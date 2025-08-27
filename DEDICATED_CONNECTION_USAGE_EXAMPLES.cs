using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Confluent.Kafka;

// DEDICATED CONNECTION USAGE EXAMPLES
// These examples show how to use dedicated consumer connections with the same API as core registration

namespace K.EntityFrameworkCore.Examples;

public class DedicatedConnectionExamples
{
    // Example 1: Basic dedicated connection with builder configuration
    public void ConfigureDedicatedConnectionWithBuilder()
    {
        // In your DbContext's OnModelCreating method:
        /*
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasName("order-created-topic");

            topic.HasConsumer(consumer =>
            {
                // Configure a dedicated connection using the builder pattern
                consumer.HasDedicatedConnection(dedicated =>
                {
                    dedicated.WithConsumerGroupId("order-processing-dedicated");
                    dedicated.WithStartImmediately(true);
                });

                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(o => new { o.OrderId, o.Status });
                    inbox.UseDeduplicationTimeWindow(TimeSpan.FromHours(1));
                });
            });
        });
        */
    }

    // Example 2: Dedicated connection with consumer configuration (same API as core registration)
    public void ConfigureDedicatedConnectionWithConsumerConfig()
    {
        // In your DbContext's OnModelCreating method:
        /*
        modelBuilder.Topic<HighVolumeEvent>(topic =>
        {
            topic.HasName("high-volume-events");

            topic.HasConsumer(consumer =>
            {
                // Configure a dedicated connection using the same API as client.Consumer
                consumer.HasDedicatedConnection(dedicatedConsumer =>
                {
                    // This API matches exactly: client.Consumer.MaxPollIntervalMs = 300000;
                    dedicatedConsumer.MaxPollIntervalMs = 300000; // 5 minutes
                    dedicatedConsumer.SessionTimeoutMs = 45000;   // 45 seconds
                    dedicatedConsumer.HeartbeatIntervalMs = 3000; // 3 seconds
                    dedicatedConsumer.GroupId = "high-volume-dedicated";
                    dedicatedConsumer.AutoOffsetReset = AutoOffsetReset.Latest;
                    dedicatedConsumer.EnableAutoCommit = false;
                    
                    // Consumer processing settings
                    dedicatedConsumer.MaxBufferedMessages = 50000;
                    dedicatedConsumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
                });

                consumer.WithMaxBufferedMessages(50000);
                consumer.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
            });
        });
        */
    }

    // Example 3: Multiple message types with different dedicated configurations
    public void ConfigureMultipleDedicatedConnections()
    {
        // In your DbContext's OnModelCreating method:
        /*
        // Critical events get dedicated high-performance consumer
        modelBuilder.Topic<CriticalAlert>(topic =>
        {
            topic.HasName("critical-alerts");
            topic.HasConsumer(consumer =>
            {
                consumer.HasDedicatedConnection(dedicatedConsumer =>
                {
                    dedicatedConsumer.GroupId = "critical-alerts-processor";
                    dedicatedConsumer.MaxPollIntervalMs = 10000;  // Fast polling
                    dedicatedConsumer.SessionTimeoutMs = 6000;
                    dedicatedConsumer.MaxBufferedMessages = 100;  // Small buffer for fast processing
                    dedicatedConsumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
                });
            });
        });

        // Bulk events get dedicated efficient consumer
        modelBuilder.Topic<BulkDataEvent>(topic =>
        {
            topic.HasName("bulk-data-events");
            topic.HasConsumer(consumer =>
            {
                consumer.HasDedicatedConnection(dedicatedConsumer =>
                {
                    dedicatedConsumer.GroupId = "bulk-data-processor";
                    dedicatedConsumer.MaxPollIntervalMs = 300000;    // Slow polling
                    dedicatedConsumer.FetchMaxBytes = 52428800;      // 50MB
                    dedicatedConsumer.MaxPartitionFetchBytes = 10485760; // 10MB
                    dedicatedConsumer.MaxBufferedMessages = 10000;   // Large buffer
                    dedicatedConsumer.BackpressureMode = ConsumerBackpressureMode.DropOldest;
                });
            });
        });

        // Regular events use shared consumer (no dedicated connection)
        modelBuilder.Topic<RegularEvent>(topic =>
        {
            topic.HasName("regular-events");
            topic.HasConsumer(consumer =>
            {
                // No dedicated connection - uses shared KafkaConsumerPollService
                consumer.WithMaxBufferedMessages(1000);
                consumer.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
            });
        });
        */
    }

    // Example 4: Dedicated connection with security configuration
    public void ConfigureDedicatedConnectionWithSecurity()
    {
        // In your DbContext's OnModelCreating method:
        /*
        modelBuilder.Topic<SecureEvent>(topic =>
        {
            topic.HasName("secure-events");
            topic.HasConsumer(consumer =>
            {
                consumer.HasDedicatedConnection(dedicatedConsumer =>
                {
                    dedicatedConsumer.GroupId = "secure-processor";
                    
                    // Security configuration (same API as core registration)
                    dedicatedConsumer.SecurityProtocol = SecurityProtocol.SaslSsl;
                    dedicatedConsumer.SaslMechanism = SaslMechanism.Plain;
                    dedicatedConsumer.SaslUsername = "secure-consumer-user";
                    dedicatedConsumer.SaslPassword = "secure-password";
                    
                    // SSL configuration
                    dedicatedConsumer.SslCaLocation = "/path/to/ca-cert";
                    dedicatedConsumer.SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https;
                    
                    // Performance tuning for secure connection
                    dedicatedConsumer.SocketTimeoutMs = 60000;
                    dedicatedConsumer.MaxBufferedMessages = 500;
                    dedicatedConsumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
                });
            });
        });
        */
    }

    // Example 5: Comparison with core registration API
    public void CompareWithCoreRegistrationAPI()
    {
        // CORE REGISTRATION API (in Program.cs):
        /*
        builder.Services.AddDbContext<MyDbContext>(optionsBuilder => optionsBuilder
            .UseSqlServer("connection-string")
            .UseKafkaExtensibility(client =>
            {
                client.BootstrapServers = "localhost:9092";
                
                // Global consumer configuration
                client.Consumer.GroupId = "my-app-group";
                client.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
                client.Consumer.EnableAutoCommit = true;
                client.Consumer.MaxBufferedMessages = 1000;
                client.Consumer.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
            }));
        */

        // DEDICATED CONNECTION API (in OnModelCreating):
        /*
        modelBuilder.Topic<MyEvent>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                // Same API structure as client.Consumer
                consumer.HasDedicatedConnection(dedicatedConsumer =>
                {
                    dedicatedConsumer.GroupId = "my-event-dedicated-group";
                    dedicatedConsumer.AutoOffsetReset = AutoOffsetReset.Latest;
                    dedicatedConsumer.EnableAutoCommit = false;
                    dedicatedConsumer.MaxBufferedMessages = 5000;
                    dedicatedConsumer.BackpressureMode = ConsumerBackpressureMode.DropOldest;
                });
            });
        });
        */
    }
}

// Example message types
public class OrderCreated
{
    public int OrderId { get; set; }
    public string Status { get; set; } = default!;
}

public class HighVolumeEvent
{
    public long EventId { get; set; }
    public byte[] Data { get; set; } = default!;
}

public class CriticalAlert
{
    public string AlertId { get; set; } = default!;
    public string Message { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}

public class BulkDataEvent
{
    public string BatchId { get; set; } = default!;
    public byte[] Payload { get; set; } = default!;
}

public class RegularEvent
{
    public string EventType { get; set; } = default!;
    public object Data { get; set; } = default!;
}

public class SecureEvent
{
    public string EventId { get; set; } = default!;
    public string EncryptedData { get; set; } = default!;
}
