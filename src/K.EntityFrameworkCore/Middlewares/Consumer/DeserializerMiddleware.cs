using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.MiddlewareOptions;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Consumer
{
    internal class DeserializerMiddleware<T>(SerializationOptions<T> options) : Middleware<T>
        where T : class
    {
        public override ValueTask InvokeAsync(IEnvelope<T> envelope, CancellationToken cancellationToken = default)
        {
            if (envelope is ISerializedEnvelope<T> serializedEnvelope && serializedEnvelope.SerializedData != null)
            {
                serializedEnvelope.Message = JsonSerializer.Deserialize<T>(serializedEnvelope.SerializedData, options.Options);
            }

            return base.InvokeAsync(envelope, cancellationToken);
        }
    }
}
