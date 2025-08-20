using K.EntityFrameworkCore.Interfaces;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Shared serialization options for a specific message type.
/// Uses compile-time type as strategy identifier - no strings needed.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class SerializationMiddlewareSettings<T>() : MiddlewareSettings<T>(isMiddlewareEnabled: true)
    where T : class
{
    private static readonly SystemTextJsonSerializer<T> defaultSerializer = new();

    public IMessageSerializer<T> Serializer { get; set; } = defaultSerializer;
    public IMessageDeserializer<T> Deserializer { get; set; } = defaultSerializer;
}

internal class SystemTextJsonSerializer<T>
    : IMessageSerializer<T, JsonSerializerOptions>
    , IMessageDeserializer<T, JsonSerializerOptions>
    where T : class
{
    public JsonSerializerOptions Options { get; } = new();

    public T? Deserialize(byte[] data)
    {
        return JsonSerializer.Deserialize<T>(data, Options);
    }

    public byte[] Serialize(in T message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, Options);
    }
}