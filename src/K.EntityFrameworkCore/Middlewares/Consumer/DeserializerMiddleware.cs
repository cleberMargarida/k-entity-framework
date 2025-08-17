using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Consumer
{
    /// <summary>
    /// Legacy deserializer middleware. Use SerializationMiddleware instead.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    [Obsolete("Use SerializationMiddleware<T> instead. This class will be removed in a future version.")]
    internal class DeserializerMiddleware<T>(SerializationMiddlewareOptions<T> options) : Middleware<T>
        where T : class
    {
        public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            byte[] serializedData = ((ISerializedEnvelope<T>)envelope).SerializedData;
            if (serializedData.Length > 0)
            {
                //envelope.Message = JsonSerializer.Deserialize<T>(serializedData, options.SystemTextJsonOptions);
            }

            return base.InvokeAsync(envelope, cancellationToken);
        }
    }
}
