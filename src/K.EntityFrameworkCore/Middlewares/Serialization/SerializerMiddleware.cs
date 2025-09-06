using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using K.EntityFrameworkCore.Middlewares.Producer;
using System.Collections.Immutable;

namespace K.EntityFrameworkCore.Middlewares.Serialization;

/// <summary>
/// Middleware that handles serialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class SerializerMiddleware<T>(SerializationMiddlewareSettings<T> settings, ProducerMiddlewareSettings<T> producerMiddlewareSettings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask<T?> InvokeAsync(scoped Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        T message = envelope.Message!;
        
        envelope.Key = producerMiddlewareSettings.GetKey(message);
        envelope.Headers = producerMiddlewareSettings.GetHeaders(message);
        envelope.Payload = settings.Serializer.Serialize(envelope.Headers, message);

        return base.InvokeAsync(envelope, cancellationToken);
    }
}
