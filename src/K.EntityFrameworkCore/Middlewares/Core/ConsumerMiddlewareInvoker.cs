using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          ConsumerMiddleware<T> consumerMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        )
    {
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(inboxMiddleware);
    }
}
