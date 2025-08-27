using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares.Core;

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

