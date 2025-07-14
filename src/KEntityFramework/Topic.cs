using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KEntityFramework;

public class Topic<T> : IConsumer<T>
{
    public Topic(
        string topicName, int partitions = 1, short replicationFactor = 1, string? cleanupPolicy = null,
        string? compressionType = null, string? deleteRetentionMs = null, string? fileDeleteDelayMs = null,
        string? flushMessages = null, string? flushMs = null, string? followerReplicationThrottledReplicas = null,
        string? indexIntervalBytes = null, string? leaderReplicationThrottledReplicas = null,
        string? maxCompactionLagMs = null, string? maxMessageBytes = null, string? messageDownconversionEnable = null,
        string? messageFormatVersion = null, string? messageTimestampType = null, string? minCleanableDirtyRatio = null,
        string? minCompactionLagMs = null, string? minInsyncReplicas = null, string? preallocate = null,
        string? retentionBytes = null, string? retentionMs = null, string? segmentBytes = null,
        string? segmentIndexBytes = null, string? segmentJitterMs = null, string? segmentMs = null,
        string? uncleanLeaderElectionEnable = null, string? messageTimestampDifferenceMaxMs = null)
    {
        TopicName = topicName;
        Partitions = partitions;
        ReplicationFactor = replicationFactor;
        //Settings = new(
        //    cleanupPolicy, compressionType, deleteRetentionMs, fileDeleteDelayMs, flushMessages, flushMs,
        //    followerReplicationThrottledReplicas, indexIntervalBytes, leaderReplicationThrottledReplicas,
        //    maxCompactionLagMs, maxMessageBytes, messageDownconversionEnable, messageFormatVersion, messageTimestampType,
        //    minCleanableDirtyRatio, minCompactionLagMs, minInsyncReplicas, preallocate, retentionBytes, retentionMs,
        //    segmentBytes, segmentIndexBytes, segmentJitterMs, segmentMs, uncleanLeaderElectionEnable,
        //    messageTimestampDifferenceMaxMs);
    }

    public string TopicName { get; }

    public int Partitions { get; }

    public short ReplicationFactor { get; }

    public TopicSettings Settings { get; }

    /// <inheritdoc/>
    public virtual void Produce(T message, IEnumerable<KeyValuePair<string, string>>? headers = default)
    {
        _ = nameof(Confluent.Kafka.IProducer<string, string>.Produce);
        throw new NotImplementedException("This method is not implemented. Use a producer to send messages to the topic.");
    }

    /// <inheritdoc/>
    public Task ProduceAsync(T message, IEnumerable<KeyValuePair<string, string>>? headers = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method is not implemented. Use a producer to send messages to the topic.");
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CommitAsync(long offset, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
