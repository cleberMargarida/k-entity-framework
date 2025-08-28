using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddlewareSettings<T>(
      IConsumerProcessingConfig globalProcessingConfig)
    : MiddlewareSettings<T>(isMiddlewareEnabled: true)
    , IConsumerProcessingConfig
    where T : class
{
    /// <inheritdoc />
    public int MaxBufferedMessages { get; set; } = globalProcessingConfig.MaxBufferedMessages;

    /// <inheritdoc />
    public ConsumerBackpressureMode BackpressureMode { get; set; } = globalProcessingConfig.BackpressureMode;
    public bool ExclusiveConnection { get; internal set; }
}

