namespace K.EntityFrameworkCore;

using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares;
using System.Collections.Generic;


/// <summary>
/// Represents a mutable envelope that holds a message of type <typeparamref name="T"/> along with its serialized data and headers.
/// This is a ref struct, meaning it lives on the stack and avoids garbage collection.
/// </summary>
/// <typeparam name="T">The type of the message contained within the envelope. Must be a class.</typeparam>
/// <param name="message">A reference to the message to be contained within the envelope. Can be null.</param>
public class Envelope<T>(T? message) 
    : IEnvelope<T>
    , ISerializedEnvelope<T>
    where T : class
{
    private readonly byte[] serializedData = [0];
    private readonly Dictionary<string, object>? headers = [];

    /// <inheritdoc/>
    public T? Message
    {
        get => message;
        set => message = value;
    }

    /// <inheritdoc/>
    Dictionary<string, object>? ISerializedEnvelope<T>.Headers => headers;

    /// <inheritdoc/>
    byte[] ISerializedEnvelope<T>.SerializedData => serializedData;
}
