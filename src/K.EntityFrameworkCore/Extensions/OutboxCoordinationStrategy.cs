namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Defines the coordination strategy for the outbox polling worker.
/// </summary>
public enum OutboxCoordinationStrategy
{
    /// <summary>
    /// Single-node coordination. No cluster awareness — one worker processes all outbox rows.
    /// This is the default strategy.
    /// </summary>
    SingleNode,

    /// <summary>
    /// (NOT YET IMPLEMENTED) Exclusive-node coordination. Clustered deployment where only one node processes outbox rows at a time.
    /// Currently behaves identically to <see cref="SingleNode"/> (placeholder for future implementation).
    /// </summary>
    [Obsolete("ExclusiveNode is not implemented — behaves like SingleNode. Do not rely on exclusivity in clustered deployments.", error: false)]
    ExclusiveNode
}
