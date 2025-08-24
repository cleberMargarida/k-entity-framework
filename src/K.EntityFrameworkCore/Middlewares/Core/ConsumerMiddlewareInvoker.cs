using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          ConsumerMiddleware<T> consumerMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        , ConsumerForgetMiddleware<T> forgetMiddleware
        )
    {
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(inboxMiddleware);
        Use(forgetMiddleware);
    }
}
