using System.Threading.Channels;

namespace K.EntityFrameworkCore.Middlewares.Core;

/// <summary>
/// Configuration options for the channel-based Kafka consumer architecture.
/// </summary>
public class KafkaConsumerChannelOptions
{
    /// <summary>
    /// The maximum number of messages that can be buffered per message type.
    /// Default is 1000. Higher values provide more buffering but use more memory.
    /// Lower values provide less buffering but may cause backpressure sooner.
    /// </summary>
    public int ChannelCapacity { get; set; } = 1000;

    /// <summary>
    /// What happens when the channel is full and a new message arrives.
    /// Default is Wait, which applies backpressure.
    /// </summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Whether to allow synchronous continuations on channel operations.
    /// Default is false to prevent blocking the poll thread.
    /// </summary>
    public bool AllowSynchronousContinuations { get; set; } = false;

    /// <summary>
    /// Creates bounded channel options from this configuration.
    /// </summary>
    internal BoundedChannelOptions ToBoundedChannelOptions()
    {
        return new BoundedChannelOptions(ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = FullMode,
            AllowSynchronousContinuations = AllowSynchronousContinuations
        };
    }
}
