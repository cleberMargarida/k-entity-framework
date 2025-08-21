using K.EntityFrameworkCore.Middlewares.Batch;
using K.EntityFrameworkCore.Middlewares.CircuitBreaker;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Retry;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializerMiddleware<T> serializationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , ProducerRetryMiddleware<T> retryMiddleware
        , ProducerCircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ProducerForgetMiddleware<T> forgetMiddleware
        , ProducerBatchMiddleware<T> batchMiddleware
        , ProducerMiddleware<T> producerMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(outboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(forgetMiddleware);
        Use(batchMiddleware);
        Use(producerMiddleware);
    }
}
