using K.EntityFrameworkCore.Middlewares.Producer;

namespace K.EntityFrameworkCore.Middlewares;

internal class ProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializationMiddleware<T> serializationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , ProducerRetryMiddleware<T> retryMiddleware
        , ProducerCircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ProducerThrottleMiddleware<T> throttleMiddleware
        , ProducerBatchMiddleware<T> batchMiddleware
        , ProducerForgetMiddleware<T> forgetMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(outboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(throttleMiddleware);
        Use(batchMiddleware);
        Use(forgetMiddleware);
    }
}
