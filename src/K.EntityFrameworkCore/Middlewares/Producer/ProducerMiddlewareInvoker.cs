using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Serialization;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Producer;

internal class ProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
    where T : class
{
    public ProducerMiddlewareInvoker(
          SerializerMiddleware<T> serializationMiddleware
        , TracePropagationMiddleware<T> tracePropagationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , ProducerForgetMiddleware<T> forgetMiddleware
        , ProducerMiddleware<T> producerMiddleware
        , IModel model
        , IServiceProvider serviceProvider
        )
    {
        Use(serializationMiddleware);

        foreach (var registration in model.GetUserProducerMiddlewares<T>())
        {
            var userMiddleware = (IMiddleware<T>)ActivatorUtilities.CreateInstance(serviceProvider, registration.MiddlewareType);
            Use(new CustomMiddleware<T>(userMiddleware));
        }

        Use(tracePropagationMiddleware);
        Use(outboxMiddleware);
        Use(forgetMiddleware);
        Use(producerMiddleware);
    }
}
