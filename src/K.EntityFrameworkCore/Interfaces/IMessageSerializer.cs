using System.Collections.Immutable;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Defines a strategy for serializing messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IMessageSerializer<T>
    where T : class
{
    /// <summary>
    /// Serializes a message into a byte array with access to message headers.
    /// </summary>
    /// <param name="headers">The message headers that can be used during serialization.</param>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A byte array representing the serialized message.</returns>
    ReadOnlySpan<byte> Serialize(IImmutableDictionary<string, string> headers, in T message);
}

/// <summary>
/// Defines a serializer with options for messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <typeparam name="TOptions">The options type.</typeparam>
public interface IMessageSerializer<T, TOptions> : IMessageSerializer<T>
    where T : class
    where TOptions : class, new()
{
    /// <summary>
    /// Gets the serializer settings.
    /// </summary>
    TOptions Options { get; }
}

/// <summary>
/// Defines a strategy for deserializing messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IMessageDeserializer<out T>
    where T : class
{
    /// <summary>
    /// Deserializes a byte array into a message instance with access to message headers.
    /// </summary>
    /// <param name="headers">The message headers that can be used during deserialization.</param>
    /// <param name="data">The byte array containing the serialized message.</param>
    /// <returns>The deserialized message instance, or <c>null</c> if deserialization fails.</returns>
    T Deserialize(IImmutableDictionary<string, string> headers, ReadOnlySpan<byte> data);
}

/// <summary>
/// Defines a deserializer with options for messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <typeparam name="TOptions">The options type.</typeparam>
public interface IMessageDeserializer<out T, TOptions> : IMessageDeserializer<T>
    where T : class
    where TOptions : class, new()
{
    /// <summary>
    /// Gets the deserializer settings.
    /// </summary>
    TOptions Options { get; }
}
