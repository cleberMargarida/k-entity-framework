using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Consumer
{
    internal class DeserializerMiddleware<T>(SerializationOptions<T> options) : Middleware<T>
        where T : class
    {
        public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            byte[] serializedData = ((ISerializedEnvelope<T>)envelope).SerializedData;
            if (serializedData.Length > 0)
            {
                envelope.Message = JsonSerializer.Deserialize<T>(serializedData, options.Options);
            }

            return base.InvokeAsync(envelope, cancellationToken);
        }
    }
}
