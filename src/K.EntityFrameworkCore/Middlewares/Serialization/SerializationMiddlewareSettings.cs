using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Collections.Immutable;

namespace K.EntityFrameworkCore.Middlewares.Serialization;

/// <summary>
/// Shared serialization options for a specific message type.
/// Uses compile-time type as strategy identifier - no strings needed.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[SingletonService]
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

    public T Deserialize(IImmutableDictionary<string, string> headers, ReadOnlySpan<byte> data)
    {
        Type type = GetType(headers);
        return JsonSerializer.Deserialize(data, type, Options) as T ?? throw new InvalidOperationException("Serialize from to null values are not allowed.");
    }

    public ReadOnlySpan<byte> Serialize(IImmutableDictionary<string, string> headers, in T message)
    {
        Type type = message.GetType();
        return JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
    }

    private static Type GetType(IImmutableDictionary<string, string> headers)
    {
        var assemblyQualifiedName = headers.GetValueOrDefault("$runtimeType") ?? headers["$type"];
        return Type.GetType(assemblyQualifiedName, throwOnError: true)!;
    }
}