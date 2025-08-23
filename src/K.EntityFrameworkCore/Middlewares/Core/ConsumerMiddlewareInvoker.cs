using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          DeserializerMiddleware<T> serializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        , ForgetMiddleware<T> forgetMiddleware
        )
    {
        Use(serializationMiddleware);
        Use(inboxMiddleware);
        Use(forgetMiddleware);
    }
}
