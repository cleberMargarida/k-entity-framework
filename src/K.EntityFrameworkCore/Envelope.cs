namespace K.EntityFrameworkCore;

using System.Collections.Immutable;

/// <summary>
/// Represents a mutable envelope that holds a message of type <typeparamref name="T"/> along with its serialized data and headers.
/// This is a ref struct, meaning it lives on the stack and avoids garbage collection.
/// </summary>
/// <typeparam name="T">The type of the message contained within the envelope. Must be a class.</typeparam>
/// <param name="message">A reference to the message to be contained within the envelope. Can be null.</param>
#nullable disable
public ref struct Envelope<T>(T message)
    where T : class
{
    /// <inheritdoc/>
    public T Message { readonly get; internal set; } = message;

    /// <inheritdoc/>
    public ImmutableDictionary<string, string> Headers { readonly get; internal set; }

    /// <inheritdoc/>
    public ReadOnlySpan<byte> Payload { readonly get; internal set; }

    ///<inheritdoc/>
    public string Key { readonly get; internal set; }

    internal WeakReference<object> WeakReference 
    { 
        get => field ??= new(null!); 
        set => field = value; 
    }

    internal void Clean()
    {
        Message = null;
        Key = null;
        Payload = null;
        Headers = null;
        WeakReference = null;
    }
}
