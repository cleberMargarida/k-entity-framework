namespace K.EntityFrameworkCore
{
    /// <summary>
    /// Represents a message stored in the outbox table
    /// for reliable event publishing.
    /// </summary>
    internal class OutboxMessage
    {
        /// <summary>
        /// Unique identifier of the outbox message.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type or name of the event/message.
        /// </summary>
        public string EventType { get; set; } = default!;

        /// <summary>
        /// Identifier of the entity or aggregate this event belongs to.
        /// </summary>
        public string AggregateId { get; set; } = default!;

        /// <summary>
        /// The serialized event payload (usually JSON).
        /// </summary>
        public string Payload { get; set; } = default!;

        /// <summary>
        /// When the event occurred (domain time).
        /// </summary>
        public DateTime OccurredOn { get; set; }

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

        /// <summary>
        /// Stores the last error message if processing failed.
        /// </summary>
        public string? LastError { get; set; }
    }
}
