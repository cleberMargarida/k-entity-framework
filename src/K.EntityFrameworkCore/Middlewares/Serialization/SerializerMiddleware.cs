using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using K.EntityFrameworkCore.Middlewares.Producer;

namespace K.EntityFrameworkCore.Middlewares.Serialization;

/// <summary>
/// Middleware that handles serialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class SerializerMiddleware<T>(SerializationMiddlewareSettings<T> settings, ProducerMiddlewareSettings<T> producerMiddlewareSettings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        T message = envelope.Message!;
        envelope.SerializeHeaders();
        envelope.SetKey(message, producerMiddlewareSettings.GetKey);
        envelope.SetSerializedData(message, settings.Serializer);

        return base.InvokeAsync(envelope, cancellationToken);
    }

}
