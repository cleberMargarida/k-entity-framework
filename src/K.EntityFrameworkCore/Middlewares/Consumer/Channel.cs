using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal abstract class Channel(IConsumerProcessingConfig globalSettings)
{
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    private System.Threading.Channels.Channel<ConsumeResult<string, byte[]>> channel => field ??= CreateChannel(Settings);

    public IConsumerProcessingConfig Settings { private get; set; } = globalSettings;

    /// <summary>
    /// Gets the number of items currently buffered in the channel.
    /// </summary>
    internal int Count => channel.Reader.Count;

    /// <summary>
    /// Gets the maximum capacity of the channel.
    /// </summary>
    internal int Capacity => Settings.MaxBufferedMessages;

    /// <summary>
    /// Gets the ratio of channel capacity at which the consumer should be paused.
    /// </summary>
    internal double HighWaterMarkRatio => Settings.HighWaterMarkRatio;

    /// <summary>
    /// Gets the ratio of channel capacity at which a paused consumer should be resumed.
    /// </summary>
    internal double LowWaterMarkRatio => Settings.LowWaterMarkRatio;

    /// <summary>
    /// Gets the high water mark count, computed from <see cref="Capacity"/> and <see cref="HighWaterMarkRatio"/>.
    /// </summary>
    internal int HighWaterMark => (int)(Capacity * HighWaterMarkRatio);

    /// <summary>
    /// Gets the low water mark count, computed from <see cref="Capacity"/> and <see cref="LowWaterMarkRatio"/>.
    /// </summary>
    internal int LowWaterMark => (int)(Capacity * LowWaterMarkRatio);

    /// <summary>
    /// Returns <see langword="true"/> when the channel count has reached or exceeded the high water mark,
    /// indicating the consumer should be paused.
    /// </summary>
    internal bool ShouldPause => Count >= HighWaterMark;

    /// <summary>
    /// Returns <see langword="true"/> when the channel count has dropped to or below the low water mark,
    /// indicating a paused consumer can be resumed.
    /// </summary>
    internal bool ShouldResume => Count <= LowWaterMark;

    public ValueTask WriteAsync(ConsumeResult<string, byte[]> result, CancellationToken cancellationToken)
    {
        return channel.Writer.WriteAsync(result, cancellationToken);
    }

    internal ValueTask<ConsumeResult<string, byte[]>> ReadAsync(CancellationToken cancellationToken)
    {
        return channel.Reader.ReadAsync(cancellationToken);
    }

    private static System.Threading.Channels.Channel<ConsumeResult<string, byte[]>> CreateChannel(IConsumerProcessingConfig settings)
    {
        return System.Threading.Channels.Channel.CreateBounded<ConsumeResult<string, byte[]>>(settings.ToBoundedChannelOptions());
    }
}

internal class Channel<T>(IConsumerProcessingConfig settings) : Channel(settings)
    where T : class
{
}