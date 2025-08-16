using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Interface for envelopes that contain serialized data.
/// </summary>
internal interface ISerializedEnvelope<T> : IEnvelope<T> where T : class
{
    new public T? Message { set; }

    /// <summary>
    /// Gets metadata headers for the serialized message.
    /// </summary>
    Dictionary<string, object>? Headers { get; }

    /// <summary>
    /// Gets the serialized data bytes.
    /// </summary>
    byte[]? SerializedData { get; }
}
