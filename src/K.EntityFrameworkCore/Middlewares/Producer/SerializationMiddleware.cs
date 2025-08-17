using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Middleware that handles serialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class SerializationMiddleware<T>(SerializationMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        ((ISerializedEnvelope<T>)envelope).SerializedData = options.SerializerInstance.Serialize(envelope.Message!);

        return base.InvokeAsync(envelope, cancellationToken);
    }
}
