using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddlewareSettings<T>(IModel model, IConsumerProcessingConfig consumerConfig) : MiddlewareSettings<T>(isMiddlewareEnabled: true), IConsumerProcessingConfig
    where T : class
{
    /// <inheritdoc />
    public int MaxBufferedMessages { get; } = model.GetMaxBufferedMessages<T>() ?? consumerConfig.MaxBufferedMessages;

    /// <inheritdoc />
    public ConsumerBackpressureMode BackpressureMode { get; } = model.GetBackpressureMode<T>() ?? consumerConfig.BackpressureMode;

    /// <inheritdoc />
    public double HighWaterMarkRatio { get; } = model.GetHighWaterMarkRatio<T>() ?? consumerConfig.HighWaterMarkRatio;

    /// <inheritdoc />
    public double LowWaterMarkRatio { get; } = model.GetLowWaterMarkRatio<T>() ?? consumerConfig.LowWaterMarkRatio;

    /// <inheritdoc />
    public bool ExclusiveConnection { get; } = model.HasExclusiveConnection<T>();
}

