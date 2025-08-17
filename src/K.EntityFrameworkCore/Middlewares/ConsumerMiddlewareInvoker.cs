using K.EntityFrameworkCore.Middlewares.Consumer;

namespace K.EntityFrameworkCore.Middlewares;

internal class ConsumerMiddlewareInvoker<T> : Middleware<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          DeserializerMiddleware<T> deserializerMiddleware
        , InboxMiddleware<T> inboxMiddleware
        , RetryMiddleware<T> retryMiddleware
        , CircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , ThrottleMiddleware<T> throttleMiddleware
        , BatchMiddleware<T> batchMiddleware
        , ForgetMiddleware<T> forgetMiddleware
        )
    {
        Use(deserializerMiddleware);    // Deserialize first
        Use(inboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(throttleMiddleware);
        Use(batchMiddleware);
        Use(forgetMiddleware);
    }
}
