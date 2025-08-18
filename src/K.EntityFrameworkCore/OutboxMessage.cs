namespace K.EntityFrameworkCore
{
    /// <summary>
    /// Represents a message stored in the outbox table
    /// for reliable event publishing.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        /// Unique identifier of the outbox message.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The sequence in which this message was generated.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// The bucket this message belongs to for partitioning.
        /// </summary>
        public string? AggregateId { get; set; }

        /// <summary>
        /// The type or name of the event/message.
        /// </summary>
        public string EventType { get; set; } = default!;
        
        /// <summary>
        /// The type or name of the message at runtime.
        /// </summary>
        public string RuntimeType { get; set; } = default!;

        /// <summary>
        /// The message headers.
        /// </summary>
        public string? Headers { get; set; } = default!;

        /// <summary>
        /// The serialized event payload.
        /// </summary>
        public byte[] Payload { get; set; } = default!;

        /// <summary>
        /// Whether the message has been processed/published.
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// When the message was processed/published (if applicable).
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Number of attempts to publish this message.
        /// </summary>
        public int Retries { get; set; }
    }
}
