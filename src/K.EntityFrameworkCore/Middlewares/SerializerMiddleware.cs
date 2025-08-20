using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Middleware that handles serialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class SerializerMiddleware<T>(SerializationMiddlewareSettings<T> settings, ProducerMiddlewareSettings<T> producerMiddlewareSettings) : Middleware<T>(settings)
    where T : class
{
    [field: AllowNull]
    private Func<T, string?> GetKey => field ??= producerMiddlewareSettings.GetKey;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        T message = envelope.Message!;
        envelope.SerializeHeaders();
        envelope.SetKey(message, GetKey);
        envelope.SetSerializedData(message, settings.Serializer);

        return base.InvokeAsync(envelope, cancellationToken);
    }

}
