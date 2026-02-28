using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddlewareSettings<T> : MiddlewareSettings<T>, IConsumerProcessingConfig
    where T : class
{
    public ConsumerMiddlewareSettings(IModel model, IConsumerProcessingConfig consumerConfig)
        : base(isMiddlewareEnabled: true)
    {
        MaxBufferedMessages = model.GetMaxBufferedMessages<T>() ?? consumerConfig.MaxBufferedMessages;
        BackpressureMode = model.GetBackpressureMode<T>() ?? consumerConfig.BackpressureMode;
        ExclusiveConnection = model.HasExclusiveConnection<T>();

        var high = model.GetHighWaterMarkRatio<T>() ?? consumerConfig.HighWaterMarkRatio;
        var low = model.GetLowWaterMarkRatio<T>() ?? consumerConfig.LowWaterMarkRatio;

        if (high <= low)
        {
            low = high * 0.5;
        }

        HighWaterMarkRatio = high;
        LowWaterMarkRatio = low;
    }

    /// <inheritdoc />
    public int MaxBufferedMessages { get; }

    /// <inheritdoc />
    public ConsumerBackpressureMode BackpressureMode { get; }

    /// <inheritdoc />
    public double HighWaterMarkRatio { get; }

    /// <inheritdoc />
    public double LowWaterMarkRatio { get; }

    /// <inheritdoc />
    public bool ExclusiveConnection { get; }
}

