using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal abstract class Channel(IConsumerProcessingConfig globalSettings)
{
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    private System.Threading.Channels.Channel<ConsumeResult<string, byte[]>> channel => field ??= CreateChannel(Settings);

    public IConsumerProcessingConfig Settings { private get; set; } = globalSettings;

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