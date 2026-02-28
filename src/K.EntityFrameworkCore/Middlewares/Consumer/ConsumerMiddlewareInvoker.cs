using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.HeaderFilter;
using K.EntityFrameworkCore.Middlewares.Inbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ConsumerMiddlewareInvoker(
          SubscriberMiddleware<T> subscriberMiddleware
        , ConsumerMiddleware<T> consumerMiddleware
        , TraceExtractionMiddleware<T> traceExtractionMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , HeaderFilterMiddleware<T> headerFilterMiddleware
        , InboxMiddleware<T> inboxMiddleware
        , IModel model
        , IServiceProvider serviceProvider)
    {
        Use(subscriberMiddleware);
        Use(consumerMiddleware);
        Use(traceExtractionMiddleware);
        Use(deserializationMiddleware);

        foreach (var registration in model.GetUserConsumerMiddlewares<T>())
        {
            var userMiddleware = (IMiddleware<T>)ActivatorUtilities.CreateInstance(serviceProvider, registration.MiddlewareType);
            Use(new CustomMiddleware<T>(userMiddleware));
        }

        Use(headerFilterMiddleware);
        Use(inboxMiddleware);
    }
}
