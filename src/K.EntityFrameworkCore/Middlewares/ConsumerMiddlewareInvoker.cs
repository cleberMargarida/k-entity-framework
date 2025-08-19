using K.EntityFrameworkCore.Middlewares.Consumer;

namespace K.EntityFrameworkCore.Middlewares;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          DeserializerMiddleware<T> serializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        , RetryMiddleware<T> retryMiddleware
        , CircuitBreakerMiddleware<T> circuitBreakerMiddleware
        , BatchMiddleware<T> batchMiddleware
        , ForgetMiddleware<T> forgetMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(inboxMiddleware);
        Use(retryMiddleware);
        Use(circuitBreakerMiddleware);
        Use(batchMiddleware);
        Use(forgetMiddleware);
    }
}
