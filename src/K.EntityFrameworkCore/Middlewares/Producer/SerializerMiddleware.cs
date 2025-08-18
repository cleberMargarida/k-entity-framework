using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.MiddlewareOptions.Producer;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Middleware that handles serialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class SerializerMiddleware<T>(SerializationMiddlewareOptions<T> options, ProducerMiddlewareOptions<T> producerMiddlewareOptions) : Middleware<T>(options)
    where T : class
{
    [field: AllowNull]
    private Func<T, string?> GetKey => field ??= producerMiddlewareOptions.GetKey;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        ISerializedEnvelope<T> envelopeSerialized = envelope;

        T message = envelope.Message!;

        SetHeaders(envelopeSerialized);
        SetKey(envelopeSerialized, message);
        SetValue(envelopeSerialized, message);

        return base.InvokeAsync(envelope, cancellationToken);
    }

    private static void SetHeaders(ISerializedEnvelope<T> envelopeSerialized)
    {
        Dictionary<string, object>? headers = envelopeSerialized.Headers;

        if (headers is null)
        {
            return;
        }

        foreach (var header in headers)
        {
            headers[header.Key] = JsonSerializer.SerializeToUtf8Bytes(header.Value);
        }
    }

    private void SetKey(ISerializedEnvelope<T> envelopeSerialized, T message)
    {
        envelopeSerialized.Key = GetKey(message);
    }

    private void SetValue(ISerializedEnvelope<T> envelopeSerialized, T message)
    {
        IMessageSerializer<T> serializer = options.SerializerInstance;
        envelopeSerialized.SerializedData = serializer.Serialize(message!);
    }
}
