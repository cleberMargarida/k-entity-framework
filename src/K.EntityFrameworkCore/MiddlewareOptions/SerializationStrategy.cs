namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Enumeration of supported serialization strategies.
/// </summary>
public enum SerializationStrategy
{
    /// <summary>
    /// Use System.Text.Json for serialization.
    /// </summary>
    SystemTextJson,

    /// <summary>
    /// Use Newtonsoft.Json for serialization.
    /// </summary>
    NewtonsoftJson,

    /// <summary>
    /// Use MessagePack for serialization.
    /// </summary>
    MessagePack
}
