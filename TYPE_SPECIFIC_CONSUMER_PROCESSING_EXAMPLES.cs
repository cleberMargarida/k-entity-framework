// TYPE_SPECIFIC_CONSUMER_PROCESSING_EXAMPLES.cs
// Examples showing how to configure different consumer processing settings per message type

using Microsoft.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Examples;

/// <summary>
/// Examples of configuring type-specific consumer processing settings.
/// Each message type can now have its own buffer size and backpressure behavior.
/// </summary>
public static class TypeSpecificConsumerProcessingExamples
{
    /// <summary>
    /// Example 1: Different processing settings per message type
    /// - High-volume messages (OrderCreated) get larger buffers
    /// - Low-volume messages (PaymentProcessed) get smaller buffers  
    /// - Critical messages (UserRegistered) use safe backpressure
    /// </summary>
    public static void ConfigurePerTypeProcessing(ModelBuilder modelBuilder)
    {
        // High-volume order events need large buffers for throughput
        modelBuilder.Topic<OrderCreated>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(5000); // Large buffer for high volume
                    processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                });
                
                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(o => new { o.OrderId });
                });
            });
        });

        // Payment events are less frequent but critical - use conservative settings
        modelBuilder.Topic<PaymentProcessed>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(500); // Smaller buffer
                    processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure); // Never drop
                });
            });
        });

        // User registration is critical and low-volume
        modelBuilder.Topic<UserRegistered>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(100); // Very conservative
                    processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                });
            });
        });
    }

    /// <summary>
    /// Example 2: Memory-constrained vs high-throughput scenarios
    /// - Bulk import messages can drop oldest when buffer is full
    /// - Transactional messages must never be dropped
    /// </summary>
    public static void MemoryOptimizedConfiguration(ModelBuilder modelBuilder)
    {
        // Bulk data import - can tolerate some message loss for performance
        modelBuilder.Topic<BulkDataImport>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(10000); // Large buffer
                    processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage); // Allow drops
                });
            });
        });

        // Financial transactions - absolutely no message loss allowed
        modelBuilder.Topic<TransactionEvent>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(1000); // Reasonable buffer
                    processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure); // Never drop
                });
                
                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(t => new { t.TransactionId });
                    inbox.UseDeduplicationTimeWindow(TimeSpan.FromDays(30)); // Long retention
                });
            });
        });
    }

    /// <summary>
    /// Example 3: Real-time vs batch processing patterns
    /// - Real-time alerts need immediate processing with small buffers
    /// - Analytics events can be batched with larger buffers
    /// </summary>
    public static void ProcessingPatternConfiguration(ModelBuilder modelBuilder)
    {
        // Real-time alerts - small buffer, immediate processing
        modelBuilder.Topic<SecurityAlert>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(50); // Very small buffer
                    processing.WithBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
                });
            });
        });

        // Analytics events - large buffer for batch processing efficiency
        modelBuilder.Topic<UserActionEvent>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(50000); // Very large buffer
                    processing.WithBackpressureMode(ConsumerBackpressureMode.DropOldestMessage); // OK to drop old analytics
                });
            });
        });
    }

    /// <summary>
    /// Example 4: Inheriting global defaults with selective overrides
    /// - Some types use global defaults (no HasProcessing configuration)
    /// - Others override specific settings as needed
    /// </summary>
    public static void SelectiveOverrideConfiguration(ModelBuilder modelBuilder)
    {
        // Uses global defaults from client.Consumer.Processing.* configuration
        modelBuilder.Topic<StandardEvent>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasInbox(inbox =>
                {
                    inbox.HasDeduplicateProperties(e => new { e.Id });
                });
                // No HasProcessing() call - uses global defaults
            });
        });

        // Override just the buffer size, keep global backpressure mode
        modelBuilder.Topic<HighVolumeEvent>(topic =>
        {
            topic.HasConsumer(consumer =>
            {
                consumer.HasProcessing(processing =>
                {
                    processing.WithMaxBufferedMessages(20000); // Override buffer size only
                    // BackpressureMode inherits from global config
                });
            });
        });
    }
}

/// <summary>
/// Example message types for demonstration
/// </summary>
public class OrderCreated
{
    public int OrderId { get; set; }
    public string Status { get; set; } = default!;
}

public class PaymentProcessed
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
}

public class UserRegistered
{
    public int UserId { get; set; }
    public string Email { get; set; } = default!;
}

public class BulkDataImport
{
    public string BatchId { get; set; } = default!;
    public byte[] Data { get; set; } = default!;
}

public class TransactionEvent
{
    public string TransactionId { get; set; } = default!;
    public decimal Amount { get; set; }
}

public class SecurityAlert
{
    public string AlertId { get; set; } = default!;
    public string Severity { get; set; } = default!;
}

public class UserActionEvent
{
    public int UserId { get; set; }
    public string Action { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}

public class StandardEvent
{
    public int Id { get; set; }
    public string Data { get; set; } = default!;
}

public class HighVolumeEvent
{
    public int Id { get; set; }
    public string Data { get; set; } = default!;
}
