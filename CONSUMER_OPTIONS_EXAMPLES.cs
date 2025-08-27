// CONSUMER_OPTIONS_EXAMPLES.cs
// Examples showing how to configure Kafka consumer options for message processing behavior

using Microsoft.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Examples;

/// <summary>
/// Examples of configuring Kafka consumer options to control message processing behavior.
/// These options are now integrated directly into the IConsumerConfig interface,
/// making them part of the standard Kafka configuration API.
/// </summary>
public static class ConsumerOptionsExamples
{
    /// <summary>
    /// Example 1: Basic consumer configuration with default settings
    /// - Uses default buffer size of 1000 messages
    /// - Applies backpressure when buffer is full (recommended for reliability)
    /// </summary>
    public static void BasicConsumerConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            client.BootstrapServers = "localhost:9092";
            client.Consumer.GroupId = "my-consumer-group";
            
            // Consumer processing options are now part of the standard consumer config
            client.Consumer.Processing.MaxBufferedMessages = 1000; // Buffer up to 1000 messages in memory
            client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure; // Safe default
        });
    }

    /// <summary>
    /// Example 2: High-throughput configuration
    /// - Larger buffer for better throughput in high-volume scenarios
    /// - Still uses backpressure to prevent memory exhaustion
    /// </summary>
    public static void HighThroughputConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            client.BootstrapServers = "localhost:9092";
            client.Consumer.GroupId = "high-throughput-group";
            
            // Increase buffer for high-throughput scenarios
            client.Consumer.Processing.MaxBufferedMessages = 5000;
            // Keep backpressure for reliability
            client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
        });
    }

    /// <summary>
    /// Example 3: Memory-constrained configuration
    /// - Smaller buffer for environments with limited memory
    /// - Uses backpressure to prevent out-of-memory issues
    /// </summary>
    public static void MemoryConstrainedConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            client.BootstrapServers = "localhost:9092";
            client.Consumer.GroupId = "memory-constrained-group";
            
            // Reduce buffer size for memory-constrained environments
            client.Consumer.Processing.MaxBufferedMessages = 100;
            client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
        });
    }

    /// <summary>
    /// Example 4: Fire-and-forget configuration with message dropping
    /// - Drops oldest messages when buffer is full
    /// - Use only when message loss is acceptable for better performance
    /// WARNING: This can result in message loss!
    /// </summary>
    public static void FireAndForgetConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            client.BootstrapServers = "localhost:9092";
            client.Consumer.GroupId = "fire-and-forget-group";
            
            client.Consumer.Processing.MaxBufferedMessages = 1000;
            // WARNING: This can cause message loss!
            client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.DropOldestMessage;
        });
    }

    /// <summary>
    /// Example 5: Minimal configuration - using all defaults
    /// - MaxBufferedMessages = 1000
    /// - BackpressureMode = ApplyBackpressure
    /// </summary>
    public static void MinimalConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            client.BootstrapServers = "localhost:9092";
            client.Consumer.GroupId = "minimal-group";
            
            // Processing options have good defaults, no need to configure them explicitly
        });
    }

    /// <summary>
    /// Example 6: Advanced configuration combining standard Kafka settings with processing options
    /// </summary>
    public static void AdvancedConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            // Standard Kafka configuration
            client.BootstrapServers = "localhost:9092";
            client.Consumer.GroupId = "advanced-group";
            client.Consumer.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
            client.Consumer.EnableAutoCommit = false; // Manual commit for better control
            
            // Consumer processing options (now integrated into consumer config)
            client.Consumer.Processing.MaxBufferedMessages = 2000;
            client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
        });
    }
}

/// <summary>
/// Migration example showing how the new integrated approach works.
/// </summary>
public static class IntegratedApproachExample
{
    /// <summary>
    /// NEW INTEGRATED APPROACH: Consumer processing options are part of IConsumerConfig
    /// This provides a unified configuration experience where all consumer-related
    /// settings are accessible through the Consumer property.
    /// </summary>
    public static void IntegratedConsumerConfiguration(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseKafkaExtensibility(client =>
        {
            client.BootstrapServers = "localhost:9092";
            
            // All consumer settings in one place
            client.Consumer.GroupId = "integrated-group";
            client.Consumer.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
            client.Consumer.EnableAutoCommit = true;
            
            // Processing options are now integrated into the consumer config
            client.Consumer.Processing.MaxBufferedMessages = 1500;
            client.Consumer.Processing.BackpressureMode = ConsumerBackpressureMode.ApplyBackpressure;
        });
    }
}
