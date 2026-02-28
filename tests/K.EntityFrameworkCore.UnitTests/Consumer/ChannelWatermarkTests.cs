using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Consumer;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Consumer;

/// <summary>
/// Tests for Channel watermark properties: ShouldPause, ShouldResume, HighWaterMark, LowWaterMark.
/// </summary>
public class ChannelWatermarkTests
{
    private sealed record TestMessage(int Id, string Name);

    [Fact]
    public void HighWaterMark_DefaultRatio_IsEightyPercentOfCapacity()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 100 };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(80, channel.HighWaterMark);
    }

    [Fact]
    public void LowWaterMark_DefaultRatio_IsFiftyPercentOfCapacity()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 100 };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(50, channel.LowWaterMark);
    }

    [Fact]
    public void HighWaterMark_CustomRatio_ComputesCorrectly()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 200,
            HighWaterMarkRatio = 0.90
        };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(180, channel.HighWaterMark);
    }

    [Fact]
    public void LowWaterMark_CustomRatio_ComputesCorrectly()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 200,
            LowWaterMarkRatio = 0.30
        };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(60, channel.LowWaterMark);
    }

    [Fact]
    public void ShouldPause_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 10 };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.False(channel.ShouldPause);
    }

    [Fact]
    public void ShouldResume_WhenEmpty_ReturnsTrue()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 10 };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.True(channel.ShouldResume);
    }

    [Fact]
    public async Task ShouldPause_WhenAtHighWaterMark_ReturnsTrue()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 10,
            HighWaterMarkRatio = 0.80 // HWM = 8
        };
        var channel = new Channel<TestMessage>(config);

        // Fill to high water mark
        for (int i = 0; i < 8; i++)
        {
            await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);
        }

        // Act & Assert
        Assert.True(channel.ShouldPause);
    }

    [Fact]
    public async Task ShouldPause_WhenBelowHighWaterMark_ReturnsFalse()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 10,
            HighWaterMarkRatio = 0.80 // HWM = 8
        };
        var channel = new Channel<TestMessage>(config);

        // Fill to just below high water mark
        for (int i = 0; i < 7; i++)
        {
            await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);
        }

        // Act & Assert
        Assert.False(channel.ShouldPause);
    }

    [Fact]
    public async Task ShouldResume_WhenAboveLowWaterMark_ReturnsFalse()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 10,
            LowWaterMarkRatio = 0.50 // LWM = 5
        };
        var channel = new Channel<TestMessage>(config);

        // Fill to above low water mark
        for (int i = 0; i < 6; i++)
        {
            await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);
        }

        // Act & Assert
        Assert.False(channel.ShouldResume);
    }

    [Fact]
    public async Task ShouldResume_WhenAtLowWaterMark_ReturnsTrue()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 10,
            LowWaterMarkRatio = 0.50 // LWM = 5
        };
        var channel = new Channel<TestMessage>(config);

        // Fill to exactly low water mark
        for (int i = 0; i < 5; i++)
        {
            await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);
        }

        // Act & Assert
        Assert.True(channel.ShouldResume);
    }

    [Fact]
    public void Capacity_ReturnsMaxBufferedMessages()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 500 };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(500, channel.Capacity);
    }

    [Fact]
    public void Count_EmptyChannel_ReturnsZero()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 10 };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(0, channel.Count);
    }

    [Fact]
    public async Task Count_AfterWrites_ReturnsCorrectCount()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig { MaxBufferedMessages = 10 };
        var channel = new Channel<TestMessage>(config);

        // Act
        await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);
        await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);
        await channel.WriteAsync(CreateConsumeResult(), CancellationToken.None);

        // Assert
        Assert.Equal(3, channel.Count);
    }

    [Fact]
    public void WatermarkRatios_ExposeConfigValues()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 100,
            HighWaterMarkRatio = 0.75,
            LowWaterMarkRatio = 0.25
        };
        var channel = new Channel<TestMessage>(config);

        // Act & Assert
        Assert.Equal(0.75, channel.HighWaterMarkRatio);
        Assert.Equal(0.25, channel.LowWaterMarkRatio);
        Assert.Equal(75, channel.HighWaterMark);
        Assert.Equal(25, channel.LowWaterMark);
    }

    private static Confluent.Kafka.ConsumeResult<string, byte[]> CreateConsumeResult()
    {
        return new Confluent.Kafka.ConsumeResult<string, byte[]>
        {
            Message = new Confluent.Kafka.Message<string, byte[]>
            {
                Key = "test-key",
                Value = System.Text.Encoding.UTF8.GetBytes("{}")
            },
            Topic = "test-topic",
            Partition = 0,
            Offset = 0
        };
    }

    /// <summary>
    /// Test implementation of <see cref="IConsumerProcessingConfig"/> for unit testing.
    /// </summary>
    private sealed class TestConsumerProcessingConfig : IConsumerProcessingConfig
    {
        public int MaxBufferedMessages { get; set; } = 1000;
        public ConsumerBackpressureMode BackpressureMode { get; set; } = ConsumerBackpressureMode.ApplyBackpressure;
        public double HighWaterMarkRatio { get; set; } = 0.80;
        public double LowWaterMarkRatio { get; set; } = 0.50;
    }
}
