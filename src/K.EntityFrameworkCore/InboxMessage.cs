namespace K.EntityFrameworkCore;

/// <summary>
/// Represents a message stored in the inbox table for deduplication of consumed messages.
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// Unique identifier of the inbox message.
    /// </summary>
    /// <remarks>
    /// The unique message identifier used for deduplication.
    /// </remarks>
    public ulong HashId { get; set; }

    /// <summary>
    /// The expiration timestamp in UTC for cleanup purposes.
    /// </summary>
    public DateTime ExpireAt { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is InboxMessage message && HashId.Equals(message.HashId);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashId.GetHashCode();
    }
}