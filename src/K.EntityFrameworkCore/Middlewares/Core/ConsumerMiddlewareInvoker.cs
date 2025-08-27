using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          IServiceProvider serviceProvider
        , DeserializerMiddleware<T> deserializationMiddleware
        , InboxMiddleware<T> inboxMiddleware
        )
    {
        // Get the correct keyed ConsumerMiddleware instance
        var consumerMiddleware = serviceProvider.GetRequiredKeyedService<ConsumerMiddleware<T>>(typeof(T));
        
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(inboxMiddleware);
    }
}
