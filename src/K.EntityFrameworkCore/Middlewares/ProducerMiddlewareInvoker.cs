using K.EntityFrameworkCore.Middlewares.Producer;

namespace K.EntityFrameworkCore.Middlewares;

internal class ProducerMiddlewareInvoker<T> : Middleware<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializerMiddleware<T> serializerMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , RetryMiddleware<T> retryMiddleware
        , CircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ThrottleMiddleware<T> throttleMiddleware
        , BatchMiddleware<T> batchMiddleware
        , AwaitForgetMiddleware<T> awaitForgetMiddleware
        , FireForgetMiddleware<T> fireForgetMiddleware
        )
    {
        Use(serializerMiddleware);      // Serialize first
        Use(outboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(throttleMiddleware);
        Use(batchMiddleware);
        Use(awaitForgetMiddleware);
        Use(fireForgetMiddleware);
    }
}
