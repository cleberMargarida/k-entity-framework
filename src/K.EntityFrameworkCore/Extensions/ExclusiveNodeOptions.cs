namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Configuration options for <c>ExclusiveNodeCoordination</c>, which uses a Kafka
/// consumer group on a single-partition topic to elect exactly one active outbox worker.
/// </summary>
public record ExclusiveNodeOptions
{
    /// <summary>
    /// Gets or sets the name of the Kafka coordination topic used for leader election.
    /// The topic is auto-created with a single partition if it does not already exist.
    /// </summary>
    /// <remarks>Default is <c>"__k_outbox_exclusive"</c>.</remarks>
    public string TopicName { get; set; } = "__k_outbox_exclusive";

    /// <summary>
    /// Gets or sets the consumer group id used for leader election.
    /// All worker nodes that share this group id participate in the same election.
    /// </summary>
    /// <remarks>Default is <c>"k-outbox-exclusive"</c>.</remarks>
    public string GroupId { get; set; } = "k-outbox-exclusive";

    /// <summary>
    /// Gets or sets the interval at which the Kafka consumer sends heartbeats
    /// to the group coordinator. Must be less than <see cref="SessionTimeout"/>.
    /// </summary>
    /// <remarks>Default is <c>3 seconds</c>.</remarks>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Gets or sets the maximum time the group coordinator waits for a heartbeat
    /// before considering the consumer dead and triggering a rebalance.
    /// </summary>
    /// <remarks>Default is <c>30 seconds</c>.</remarks>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
