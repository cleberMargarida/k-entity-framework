using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializerMiddleware<T> serializationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , ProducerForgetMiddleware<T> forgetMiddleware
        , ProducerMiddleware<T> producerMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(outboxMiddleware);
        Use(forgetMiddleware);
        Use(producerMiddleware);
    }
}
