using K.EntityFrameworkCore.Middlewares.Consumer;

namespace K.EntityFrameworkCore.Middlewares;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          DeserializationMiddleware<T> serializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        , RetryMiddleware<T> retryMiddleware
        , CircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ThrottleMiddleware<T> throttleMiddleware
        , BatchMiddleware<T> batchMiddleware
        , ForgetMiddleware<T> forgetMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(inboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(throttleMiddleware);
        Use(batchMiddleware);
        Use(forgetMiddleware);
    }
}
