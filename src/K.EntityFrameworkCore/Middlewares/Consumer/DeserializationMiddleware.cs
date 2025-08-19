using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Middleware that handles deserialization based on context.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[ScopedService]
internal class DeserializationMiddleware<T>(SerializationMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        options.Deserializer.DeserializeMessage(envelope);
        return base.InvokeAsync(envelope, cancellationToken);
    }
}
