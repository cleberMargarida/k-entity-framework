using System.Collections.Generic;

namespace KEntityFramework;

public class TopicSettings
{
    readonly Dictionary<string, string> configs = [];

    public string? CleanupPolicy
    {
        get => GetConfig("cleanup.policy");
        set => SetConfig("cleanup.policy", value);
    }

    public string? CompressionType
    {
        get => GetConfig("compression.type");
        set => SetConfig("compression.type", value);
    }

    public string? DeleteRetentionMs
    {
        get => GetConfig("delete.retention.ms");
        set => SetConfig("delete.retention.ms", value);
    }

    public string? FileDeleteDelayMs
    {
        get => GetConfig("file.delete.delay.ms");
        set => SetConfig("file.delete.delay.ms", value);
    }

    public string? FlushMessages
    {
        get => GetConfig("flush.messages");
        set => SetConfig("flush.messages", value);
    }

    public string? FlushMs
    {
        get => GetConfig("flush.ms");
        set => SetConfig("flush.ms", value);
    }

    public string? FollowerReplicationThrottledReplicas
    {
        get => GetConfig("follower.replication.throttled.replicas");
        set => SetConfig("follower.replication.throttled.replicas", value);
    }

    public string? IndexIntervalBytes
    {
        get => GetConfig("index.interval.bytes");
        set => SetConfig("index.interval.bytes", value);
    }

    public string? LeaderReplicationThrottledReplicas
    {
        get => GetConfig("leader.replication.throttled.replicas");
        set => SetConfig("leader.replication.throttled.replicas", value);
    }

    public string? MaxCompactionLagMs
    {
        get => GetConfig("max.compaction.lag.ms");
        set => SetConfig("max.compaction.lag.ms", value);
    }

    public string? MaxMessageBytes
    {
        get => GetConfig("max.message.bytes");
        set => SetConfig("max.message.bytes", value);
    }

    public string? MessageDownconversionEnable
    {
        get => GetConfig("message.downconversion.enable");
        set => SetConfig("message.downconversion.enable", value);
    }

    public string? MessageFormatVersion
    {
        get => GetConfig("message.format.version");
        set => SetConfig("message.format.version", value);
    }

    public string? MessageTimestampType
    {
        get => GetConfig("message.timestamp.type");
        set => SetConfig("message.timestamp.type", value);
    }

    public string? MinCleanableDirtyRatio
    {
        get => GetConfig("min.cleanable.dirty.ratio");
        set => SetConfig("min.cleanable.dirty.ratio", value);
    }

    public string? MinCompactionLagMs
    {
        get => GetConfig("min.compaction.lag.ms");
        set => SetConfig("min.compaction.lag.ms", value);
    }

    public string? MinInsyncReplicas
    {
        get => GetConfig("min.insync.replicas");
        set => SetConfig("min.insync.replicas", value);
    }

    public string? Preallocate
    {
        get => GetConfig("preallocate");
        set => SetConfig("preallocate", value);
    }

    public string? RetentionBytes
    {
        get => GetConfig("retention.bytes");
        set => SetConfig("retention.bytes", value);
    }

    public string? RetentionMs
    {
        get => GetConfig("retention.ms");
        set => SetConfig("retention.ms", value);
    }

    public string? SegmentBytes
    {
        get => GetConfig("segment.bytes");
        set => SetConfig("segment.bytes", value);
    }

    public string? SegmentIndexBytes
    {
        get => GetConfig("segment.index.bytes");
        set => SetConfig("segment.index.bytes", value);
    }

    public string? SegmentJitterMs
    {
        get => GetConfig("segment.jitter.ms");
        set => SetConfig("segment.jitter.ms", value);
    }

    public string? SegmentMs
    {
        get => GetConfig("segment.ms");
        set => SetConfig("segment.ms", value);
    }

    public string? UncleanLeaderElectionEnable
    {
        get => GetConfig("unclean.leader.election.enable");
        set => SetConfig("unclean.leader.election.enable", value);
    }

    public string? MessageTimestampDifferenceMaxMs
    {
        get => GetConfig("message.timestamp.difference.max.ms");
        set => SetConfig("message.timestamp.difference.max.ms", value);
    }

    string? GetConfig(string key)
    {
        return configs.TryGetValue(key, out var value) ? value : null;
    }

    void SetConfig(string key, string? value)
    {
        if (value == null)
        {
            configs.Remove(key);
        }
        else
        {
            configs[key] = value;
        }
    }
}
