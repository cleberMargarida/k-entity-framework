using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.HeaderFilter;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          SubscriberMiddleware<T> subscriberMiddleware
        , ConsumerMiddleware<T> consumerMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , HeaderFilterMiddleware<T> headerFilterMiddleware
        , InboxMiddleware<T> inboxMiddleware)
    {
        Use(subscriberMiddleware);
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(headerFilterMiddleware);
        Use(inboxMiddleware);
    }
}
