using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.UnitTests.Helpers;

/// <summary>
/// Shared test implementation of <see cref="IConsumerProcessingConfig"/> for unit testing.
/// </summary>
internal sealed class TestConsumerProcessingConfig : IConsumerProcessingConfig
{
    public int MaxBufferedMessages { get; set; } = 1000;
    public ConsumerBackpressureMode BackpressureMode { get; set; } = ConsumerBackpressureMode.ApplyBackpressure;
    public double HighWaterMarkRatio { get; set; } = 0.80;
    public double LowWaterMarkRatio { get; set; } = 0.50;
}
