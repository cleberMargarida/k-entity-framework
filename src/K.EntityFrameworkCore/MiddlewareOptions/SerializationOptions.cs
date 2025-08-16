using System.Text.Json;

namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Shared JSON serialization options for a specific message type.
/// Used by both SerializerMiddleware and DeserializerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class SerializationOptions<T> : MiddlewareOptions<T>
    where T : class
{
    /// <summary>
    /// Gets or sets the JSON serializer options.
    /// </summary>
    public JsonSerializerOptions Options { get; set; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
