using System.Collections.Immutable;

namespace K.EntityFrameworkCore;

/// <summary>
/// Represents a mutable envelope that encapsulates a message of type <typeparamref name="T"/> along with its serialized payload, headers, and routing key.
/// Used throughout the middleware pipeline for processing messages in Entity Framework Core extensions for Kafka.
/// This is a ref struct, meaning it lives on the stack and avoids garbage collection overhead, making it efficient for high-throughput messaging scenarios.
/// </summary>
/// <typeparam name="T">The type of the message contained within the envelope. Must be a reference type (class) to ensure proper serialization and middleware processing.</typeparam>
/// <param name="message">The initial message instance to be encapsulated. This can be null if the envelope will be populated later in the middleware pipeline.</param>
#nullable disable
public ref struct Envelope<T>(T message)
    where T : class
{
    /// <summary>
    /// The original message object being processed through the middleware pipeline.
    /// </summary>
    public T Message { readonly get; internal set; } = message;

    /// <summary>
    /// A collection of key-value pairs containing metadata associated with the message, such as correlation IDs, timestamps, or custom headers.
    /// </summary>
    public ImmutableDictionary<string, string> Headers { readonly get; internal set; }

    /// <summary>
    /// The serialized binary representation of the message, ready for transmission over Kafka.
    /// </summary>
    public ReadOnlySpan<byte> Payload { readonly get; internal set; }

    /// <summary>
    /// The routing key used by Kafka to determine partition assignment for the message.
    /// </summary>
    public string Key { readonly get; internal set; }

    /// <summary>
    /// Internal weak reference used for tracking the envelope instance during asynchronous operations, allowing for cleanup without preventing garbage collection.
    /// </summary>
    internal WeakReference<object> WeakReference
    {
        get => field ??= new(null);
        set => field = value;
    }
}
