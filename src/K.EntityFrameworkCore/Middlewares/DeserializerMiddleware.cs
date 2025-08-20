using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Middleware that handles deserialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class DeserializerMiddleware<T>(SerializationMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        //TODO deserialize runtime type
        settings.Deserializer.DeserializeMessage(envelope);
        return base.InvokeAsync(envelope, cancellationToken);
    }
}
