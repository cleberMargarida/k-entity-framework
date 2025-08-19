using K.EntityFrameworkCore.Middlewares.Producer;
using System.Runtime.Intrinsics.X86;

namespace K.EntityFrameworkCore.Middlewares;

internal class ProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializerMiddleware<T> serializationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , ProducerRetryMiddleware<T> retryMiddleware
        , ProducerCircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ProducerThrottleMiddleware<T> throttleMiddleware
        , ProducerBatchMiddleware<T> batchMiddleware
        , ProducerForgetMiddleware<T> forgetMiddleware
        , ProducerMiddleware<T> producerMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(outboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(throttleMiddleware);
        Use(batchMiddleware);
        Use(forgetMiddleware);
        Use(producerMiddleware);
    }
}

internal class OutboxProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public OutboxProducerMiddlewareInvoker(
        /*
         registry custom middlewares for outbox
         */
        )
    {
    }
}
