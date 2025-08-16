using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class ConsumerMiddlewareInvoker<T> : Middleware<T>
    where T : class
{
    private ConsumerMiddlewareInvoker(params Middleware<T>[] middlewares)
    {
        foreach (var middleware in middlewares)
        {
            if (middleware.IsEnabled) Use(middleware);
        }
    }

    public ConsumerMiddlewareInvoker(
          InboxMiddleware<T> inboxMiddleware
        , RetryMiddleware<T> retryMiddleware
        , ) 
        : this(retryMiddleware, inboxMiddleware)
    {
    }
}
