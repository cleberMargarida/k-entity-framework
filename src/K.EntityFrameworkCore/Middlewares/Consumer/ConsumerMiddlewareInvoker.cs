using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.HeaderFilter;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

[ScopedService]
internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          SubscriptionMiddleware<T> subscriptionMiddleware
        , PollingMiddleware<T> pollingMiddleware
        , ConsumerMiddleware<T> consumerMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , HeaderFilterMiddleware<T> headerFilterMiddleware
        , InboxMiddleware<T> inboxMiddleware
        )
    {
        Use(subscriptionMiddleware);
        Use(pollingMiddleware);
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(headerFilterMiddleware);
        Use(inboxMiddleware);
    }
}
