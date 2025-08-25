namespace K.EntityFrameworkCore;

/// <summary>
/// Represents a message stored in the inbox table for deduplication of consumed messages.
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// Unique identifier of the inbox message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The sequence in which this message was received.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// The unique message identifier used for deduplication.
    /// </summary>
    public string MessageId { get; set; } = default!;

    /// <summary>
    /// When the message was first received for cleanup purposes.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}