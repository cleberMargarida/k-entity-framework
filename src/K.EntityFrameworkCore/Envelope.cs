namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

/// <summary>
/// Represents a mutable envelope that holds a message of type <typeparamref name="T"/> along with its serialized data and headers.
/// This is a ref struct, meaning it lives on the stack and avoids garbage collection.
/// </summary>
/// <typeparam name="T">The type of the message contained within the envelope. Must be a class.</typeparam>
/// <param name="message">A reference to the message to be contained within the envelope. Can be null.</param>
public class Envelope<T>(T? message)
    : IEnvelope<T>
    , ISerializedEnvelope<T>
    , IInfrastructure<WeakReference<OutboxMessage?>>
    where T : class
{
    private Envelope()
        : this(null)
    {
    }

    private string? key;
    private byte[] serializedData = [];//TODO instantiate?
    private Dictionary<string, object>? headers;

    /// <inheritdoc/>
    [NotMapped]
    public T? Message
    {
        get => message;
        set => message = value;
    }

    /// <inheritdoc/>
    [NotMapped]
    Dictionary<string, object>? ISerializedEnvelope<T>.Headers
    {
        get => headers;
        set => headers = value;
    }

    /// <inheritdoc/>
    [NotMapped]
    byte[] ISerializedEnvelope<T>.SerializedData
    {
        get => serializedData;
        set => serializedData = value;
    }

    string? ISerializedEnvelope<T>.Key { get => key; set => key = value; }

    /// <inheritdoc/>
    [field: AllowNull]
    WeakReference<OutboxMessage?> IInfrastructure<WeakReference<OutboxMessage?>>.Instance
    {
        get => field ??= new WeakReference<OutboxMessage?>(null);
    }
}


/// <summary>
/// Centralized manager for envelope operations including creating, sealing, unsealing, 
/// and handling serialization/deserialization of headers.
/// </summary>
internal static class EnvelopeExtensions
{
    /// <summary>
    /// Creates a new envelope with the specified message (boxing/sealing).
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to envelope. Can be null.</param>
    /// <returns>A new envelope containing the message.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Envelope<T> Seal<T>(this T? message) where T : class
    {
        return new Envelope<T>(message);
    }

    /// <summary>
    /// Extracts the message from an envelope (unboxing/unsealing).
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to extract from.</param>
    /// <returns>The message contained in the envelope, or null if no message.</returns>
    public static T? Unseal<T>(this Envelope<T> envelope) where T : class
    {
        return envelope.Message;
    }

    /// <summary>
    /// Checks if an envelope contains a valid message.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to check.</param>
    /// <returns>True if the envelope contains a non-null message.</returns>
    public static bool HasMessage<T>(this Envelope<T> envelope) where T : class
    {
        return envelope.Message is not null;
    }

    /// <summary>
    /// Serializes headers in an envelope by converting objects to byte arrays.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope containing headers to serialize.</param>
    public static void SerializeHeaders<T>(this ISerializedEnvelope<T> envelope) where T : class
    {
        Dictionary<string, object>? headers = envelope.Headers;

        if (headers is null)
        {
            return;
        }

        foreach (var header in headers)
        {
            headers[header.Key] = JsonSerializer.SerializeToUtf8Bytes(header.Value);
        }
    }

    /// <summary>
    /// Deserializes headers from an OutboxMessage into an envelope.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="outboxMessageHeaders">The headers serialized headers from outbox message.</param>
    /// <param name="envelope">The envelope to populate with deserialized headers.</param>
    public static void DeserializeHeadersFromJson<T>(this ISerializedEnvelope<T> envelope, string? outboxMessageHeaders)
        where T : class
    {
        Dictionary<string, string>? headers = outboxMessageHeaders is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(outboxMessageHeaders)!;

        envelope.Headers = headers?.ToDictionary(
            h => h.Key,
            h => (object)Convert.FromBase64String(h.Value));
    }

    /// <summary>
    /// Serializes headers from an envelope for storage in an OutboxMessage.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope containing headers to serialize.</param>
    /// <returns>Serialized headers as JSON string, or null if no headers.</returns>
    public static string? SerializeHeadersToJson<T>(this ISerializedEnvelope<T> envelope) where T : class
    {
        Dictionary<string, string>? headers = envelope.Headers?.ToDictionary(
            h => h.Key,
            h => Convert.ToBase64String((byte[])h.Value));

        return headers?.Count > 0 ? JsonSerializer.Serialize(headers) : null;
    }

    /// <summary>
    /// Creates an envelope from an OutboxMessage by populating all relevant fields.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="outboxMessage">The outbox message to convert.</param>
    /// <returns>A new envelope populated with data from the outbox message.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Envelope<T> ToEnvelope<T>(this OutboxMessage outboxMessage) where T : class
    {
        Envelope<T> envelope = new(null);

        envelope.SetKey(outboxMessage.AggregateId!);
        envelope.DeserializeHeadersFromJson(outboxMessage.Headers);
        envelope.SetSerializedData(outboxMessage.Payload);
        envelope.GetInfrastructure().SetTarget(outboxMessage);
        return envelope;
    }

    /// <summary>
    /// Creates an OutboxMessage from an envelope by extracting all relevant fields.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to convert.</param>
    /// <returns>A new OutboxMessage populated with data from the envelope.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static OutboxMessage ToOutboxMessage<T>(this ISerializedEnvelope<T> envelope) where T : class
    {
        Type runtimeType = envelope.Message!.GetType();
        Type compiledType = typeof(T);

        string? runtimeTypeAssemblyQualifiedName;
        if (runtimeType == compiledType)
        {
            runtimeTypeAssemblyQualifiedName = null;
        }
        else
        {
            runtimeTypeAssemblyQualifiedName = envelope.Message.GetType().AssemblyQualifiedName!;
        }

        return new OutboxMessage
        {
            Type = compiledType.AssemblyQualifiedName!,
            RuntimeType = runtimeTypeAssemblyQualifiedName,
            Payload = envelope.SerializedData,
            Headers = SerializeHeadersToJson(envelope),
            AggregateId = envelope.Key,
        };
    }

    /// <summary>
    /// Sets the serialized data in an envelope using the provided serializer.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to update.</param>
    /// <param name="message">The message to serialize.</param>
    /// <param name="serializer">The serializer to use.</param>
    public static void SetSerializedData<T>(this ISerializedEnvelope<T> envelope, T message, IMessageSerializer<T> serializer)
        where T : class
    {
        envelope.SerializedData = serializer.Serialize(message);
    }

    /// <summary>
    /// Sets the key in an envelope using the provided key extraction function.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to update.</param>
    /// <param name="message">The message to extract the key from.</param>
    /// <param name="keyExtractor">The function to extract the key.</param>
    public static void SetKey<T>(this ISerializedEnvelope<T> envelope, T message, Func<T, string?> keyExtractor)
        where T : class
    {
        envelope.Key = keyExtractor(message);
    }
    
    /// <summary>
    /// Sets the key in an envelope using the provided key extraction function.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to update.</param>
    /// <param name="key">The key.</param>
    public static void SetKey<T>(this ISerializedEnvelope<T> envelope, string? key)
        where T : class
    {
        envelope.Key = key;
    }
    
    /// <summary>
    /// Sets the key in an envelope using the provided key extraction function.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to update.</param>
    /// <param name="payload">The raw payload data.</param>
    public static void SetSerializedData<T>(this ISerializedEnvelope<T> envelope, byte[] payload)
        where T : class
    {
        envelope.SerializedData = payload;
    }

    /// <summary>
    /// Deserializes message data in an envelope using the provided deserializer.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="envelope">The envelope to update.</param>
    /// <param name="deserializer">The deserializer to use.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void DeserializeMessage<T>(this IMessageDeserializer<T> deserializer, Envelope<T> envelope)
        where T : class
    {
        var serializedData = ((ISerializedEnvelope<T>)envelope).SerializedData;
        envelope.Message = deserializer.Deserialize(serializedData);
    }
}
