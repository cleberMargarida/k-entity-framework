using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a message envelope that contains a message of type <typeparamref name="T"/>.
/// </summary>
public interface IEnvelope<out T>
{
    /// <summary>
    /// Gets the message contained in the envelope.
    /// </summary>
    T? Message { get; }
}
