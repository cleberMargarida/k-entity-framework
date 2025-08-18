using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Middleware that handles serialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class SerializerMiddleware<T>(SerializationMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        ISerializedEnvelope<T> envelopeSerialized = envelope;

        Dictionary<string, object> headers = envelopeSerialized.Headers;
        foreach (var header in headers)
        {
            headers[header.Key] = JsonSerializer.SerializeToUtf8Bytes(header.Value);
        }

        IMessageSerializer<T> serializer = options.SerializerInstance;
        envelopeSerialized.SerializedData = serializer.Serialize(envelope.Message!);

        return base.InvokeAsync(envelope, cancellationToken);
    }
}
