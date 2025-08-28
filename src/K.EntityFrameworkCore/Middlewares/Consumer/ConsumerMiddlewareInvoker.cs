using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          SubscriptionMiddleware<T> subscriptionMiddleware
        , PollingMiddleware<T> pollingMiddleware
        , ConsumerMiddleware<T> consumerMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        )
    {
        Use(subscriptionMiddleware);
        Use(pollingMiddleware);
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(inboxMiddleware);
    }
}
