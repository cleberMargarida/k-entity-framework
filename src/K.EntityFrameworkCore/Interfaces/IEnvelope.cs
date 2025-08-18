using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a message envelope that can be used to encapsulate messages in a middleware pipeline.
/// </summary>
public interface IEnvelope
{
}

/// <summary>
/// Represents a message envelope that contains a message of type <typeparamref name="T"/>.
/// </summary>
public interface IEnvelope<out T> : IEnvelope
{
    /// <summary>
    /// Gets the message contained in the envelope.
    /// </summary>
    T? Message { get; }
}
