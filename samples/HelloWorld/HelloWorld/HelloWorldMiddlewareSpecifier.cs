using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Producer;

namespace HelloWorld
{
    public class HelloWorldMiddlewareSpecifier : IMiddlewareSpecifier<MyDbContext>
    {
        public ScopedCommand DeferedExecution(OutboxMessage outboxMessage) => outboxMessage.EventType switch
        {
            "HelloWorld.OrderCreated, HelloWorld, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" => OutboxPollingWorker<MyDbContext>.DeferedExecution<OrderCreated>(outboxMessage),
            _ => null
        };
    }
}
