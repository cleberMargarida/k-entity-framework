using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Interface for envelopes that contain serialized data.
/// </summary>
public interface ISerializedEnvelope<T> : IEnvelope<T> where T : class
{
    /// <summary>
    /// Gets the serialized data bytes.
    /// </summary>
    byte[] SerializedData { get; }

    /// <summary>
    /// Gets metadata headers for the serialized message.
    /// </summary>
    IDictionary<string, object> Headers { get; }
}
