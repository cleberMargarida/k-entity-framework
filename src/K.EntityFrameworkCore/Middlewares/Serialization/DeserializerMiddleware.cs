using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Serialization;

/// <summary>
/// Middleware that handles deserialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class DeserializerMiddleware<T>(SerializationMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask<T?> InvokeAsync(scoped Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        envelope.Message = settings.Deserializer.Deserialize(envelope.Headers, envelope.Payload);
        return base.InvokeAsync(envelope, cancellationToken);
    }
}
