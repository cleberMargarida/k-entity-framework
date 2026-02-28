using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Consumer;
using K.EntityFrameworkCore.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Consumer;

/// <summary>
/// Tests for consumer pause/resume logic in <see cref="ConsumerPollRegistry"/>.
/// </summary>
public class ConsumerPollRegistryPauseResumeTests
{
    private sealed record TestMessage(int Id, string Name);

    [Fact]
    public async Task Register_WhenChannelReachesHighWaterMark_PausesConsumer()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 10,
            HighWaterMarkRatio = 0.80, // HWM = 8
            LowWaterMarkRatio = 0.50   // LWM = 5
        };

        var channel = new Channel<TestMessage>(config);
        var consumer = new FakeKafkaConsumer(messageCount: 9); // Produce 9 messages (above HWM of 8)

        var services = new ServiceCollection();
        services.AddSingleton<Channel<TestMessage>>(channel);
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<ConsumerPollRegistry>.Instance;
        var registry = new ConsumerPollRegistry(logger, serviceProvider);

        // Act
        registry.Register<TestMessage>(consumer);

        // Wait for messages to be processed and pause to trigger
        await WaitForConditionAsync(() => consumer.WasPaused);

        // Assert — consumer should have been paused
        Assert.True(consumer.WasPaused, "Consumer should have been paused when channel reached high water mark.");

        registry.Dispose();
    }

    [Fact]
    public async Task Register_WhenChannelDrainsBelowLowWaterMark_ResumesConsumer()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 10,
            HighWaterMarkRatio = 0.80, // HWM = 8
            LowWaterMarkRatio = 0.50   // LWM = 5
        };

        var channel = new Channel<TestMessage>(config);
        var consumer = new FakeKafkaConsumer(messageCount: 9); // Enough to trigger pause

        var services = new ServiceCollection();
        services.AddSingleton<Channel<TestMessage>>(channel);
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<ConsumerPollRegistry>.Instance;
        var registry = new ConsumerPollRegistry(logger, serviceProvider);

        // Act
        registry.Register<TestMessage>(consumer);

        // Wait for messages to fill channel and trigger pause
        await WaitForConditionAsync(() => consumer.WasPaused);

        // Drain the channel below LWM
        for (int i = 0; i < 5; i++)
        {
            await channel.ReadAsync(CancellationToken.None);
        }

        // Wait for resume check cycle
        await WaitForConditionAsync(() => consumer.WasResumed);

        // Assert — consumer should have been resumed after drain
        Assert.True(consumer.WasResumed, "Consumer should have been resumed when channel drained below low water mark.");

        registry.Dispose();
    }

    [Fact]
    public async Task Register_WhenChannelBelowHighWaterMark_DoesNotPause()
    {
        // Arrange
        var config = new TestConsumerProcessingConfig
        {
            MaxBufferedMessages = 100,
            HighWaterMarkRatio = 0.80, // HWM = 80
            LowWaterMarkRatio = 0.50
        };

        var channel = new Channel<TestMessage>(config);
        var consumer = new FakeKafkaConsumer(messageCount: 5); // Well below HWM

        var services = new ServiceCollection();
        services.AddSingleton<Channel<TestMessage>>(channel);
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<ConsumerPollRegistry>.Instance;
        var registry = new ConsumerPollRegistry(logger, serviceProvider);

        // Act
        registry.Register<TestMessage>(consumer);

        // Wait enough time for messages to be consumed; consumer should not be paused
        await WaitForConditionAsync(() => consumer.ConsumedAll, timeoutMs: 3_000);

        // Assert — consumer should NOT have been paused
        Assert.False(consumer.WasPaused, "Consumer should not be paused when channel is below high water mark.");

        registry.Dispose();
    }



    /// <summary>
    /// Minimal fake implementation of <see cref="IConsumer{TKey, TValue}"/> for testing pause/resume behavior.
    /// </summary>
    private sealed class FakeKafkaConsumer : IConsumer<string, byte[]>
    {
        private readonly List<ConsumeResult<string, byte[]>> messages = [];
        private int consumeIndex;

        public bool WasPaused { get; private set; }
        public bool WasResumed { get; private set; }
        public bool ConsumedAll => consumeIndex >= messages.Count;
        public int PauseCount { get; private set; }
        public int ResumeCount { get; private set; }

        public FakeKafkaConsumer(int messageCount)
        {
            for (int i = 0; i < messageCount; i++)
            {
                messages.Add(new ConsumeResult<string, byte[]>
                {
                    Message = new Message<string, byte[]>
                    {
                        Key = $"key-{i}",
                        Value = System.Text.Encoding.UTF8.GetBytes($"{{\"Id\":{i},\"Name\":\"msg-{i}\"}}")
                    },
                    Topic = "test-topic",
                    Partition = 0,
                    Offset = i
                });
            }
        }

        public ConsumeResult<string, byte[]> Consume(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (consumeIndex < messages.Count)
            {
                return messages[consumeIndex++];
            }

            // Block until cancelled (simulates waiting for messages)
            cancellationToken.WaitHandle.WaitOne(Timeout.Infinite);
            cancellationToken.ThrowIfCancellationRequested();
            return null!;
        }

        public ConsumeResult<string, byte[]> Consume(TimeSpan timeout)
        {
            // Heartbeat consume — return null (no messages)
            Thread.Sleep(Math.Min((int)timeout.TotalMilliseconds, 50));
            return null!;
        }

        public ConsumeResult<string, byte[]> Consume(int millisecondsTimeout)
        {
            Thread.Sleep(Math.Min(millisecondsTimeout, 50));
            return null!;
        }

        public void Pause(IEnumerable<TopicPartition> partitions)
        {
            WasPaused = true;
            PauseCount++;
        }

        public void Resume(IEnumerable<TopicPartition> partitions)
        {
            WasResumed = true;
            ResumeCount++;
        }

        public List<TopicPartition> Assignment => [new TopicPartition("test-topic", 0)];

        public Handle Handle => throw new NotImplementedException();
        public string Name => "fake-consumer";
        public string MemberId => "fake-member";
        public List<string> Subscription => [];
        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotImplementedException();

        public int AddBrokers(string brokers) => 0;
        public void Assign(TopicPartition partition) { }
        public void Assign(TopicPartitionOffset partitionOffset) { }
        public void Assign(IEnumerable<TopicPartitionOffset> partitions) { }
        public void Assign(IEnumerable<TopicPartition> partitions) { }
        public void Close() { }
        public List<TopicPartitionOffset> Commit() => [];
        public void Commit(IEnumerable<TopicPartitionOffset> offsets) { }
        public void Commit(ConsumeResult<string, byte[]> result) { }
        public List<TopicPartitionOffset> Committed(TimeSpan timeout) => [];
        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) => [];
        public void Dispose() { }
        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => throw new NotImplementedException();
        public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) { }
        public void IncrementalAssign(IEnumerable<TopicPartition> partitions) { }
        public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) { }
        public void SetSaslCredentials(string username, string password) { }
        public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) => [];
        public Offset Position(TopicPartition partition) => Offset.Unset;
        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) => throw new NotImplementedException();
        public void Seek(TopicPartitionOffset tpo) { }
        public void StoreOffset(ConsumeResult<string, byte[]> result) { }
        public void StoreOffset(TopicPartitionOffset offset) { }
        public void Subscribe(IEnumerable<string> topics) { }
        public void Subscribe(string topic) { }
        public void Unassign() { }
        public void Unsubscribe() { }
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns <c>true</c> or the timeout elapses.
    /// </summary>
    private static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 5_000, int pollIntervalMs = 50)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
                return;
            await Task.Delay(pollIntervalMs);
        }

        Assert.Fail($"Condition was not met within {timeoutMs}ms.");
    }
}
