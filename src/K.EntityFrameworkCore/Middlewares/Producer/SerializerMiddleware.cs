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
        T message = envelope.Message!;
        envelope.SerializeHeaders();
        envelope.SetKey(message, GetKey);
        envelope.SetSerializedData(message, options.Serializer);

        return base.InvokeAsync(envelope, cancellationToken);
    }

}
