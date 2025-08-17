using K.EntityFrameworkCore.Middlewares.Producer;

namespace K.EntityFrameworkCore.Middlewares;

internal class ProducerMiddlewareInvoker<T> : Middleware<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializationMiddleware<T> serializationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , RetryMiddleware<T> retryMiddleware
        , CircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ThrottleMiddleware<T> throttleMiddleware
        , BatchMiddleware<T> batchMiddleware
        , ForgetMiddleware<T> forgetMiddleware
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
