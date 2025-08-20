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
        , ProducerBatchMiddleware<T> batchMiddleware
        , ProducerForgetMiddleware<T> forgetMiddleware
        , ProducerMiddleware<T> producerMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(outboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(batchMiddleware);
        Use(forgetMiddleware);
        Use(producerMiddleware);
    }
}
internal class OutboxProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public OutboxProducerMiddlewareInvoker(
          ProducerRetryMiddleware<T> retryMiddleware
        , ProducerCircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ProducerBatchMiddleware<T> batchMiddleware
        )
    {
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(batchMiddleware);
    }
}
