using K.EntityFrameworkCore.Middlewares.Batch;
using K.EntityFrameworkCore.Middlewares.CircuitBreaker;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Retry;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Core;

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
