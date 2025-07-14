using HelloWorld;
using KEntityFramework;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBrokerContext<MyBrokerContext>("localhost:9092");

using var app = builder.Build();

app.Start();

var kafka = app.Services.GetRequiredService<MyBrokerContext>();

/*
publish synchrounsly
kafka.OrderEvents.Produce(new OrderEvent());

publish asynchronously but without specifying the topic
await kafka.ProduceAsync(new OrderEvent());

publish synchronously but without specifying the topic
kafka.Produce(new OrderEvent());
 */
await kafka.OrderEvents.ProduceAsync(new OrderEvent());

var result =  await kafka.OrderEvents.FirstAsync();

await kafka.OrderEvents.CommitAsync();

app.WaitForShutdown();

namespace HelloWorld
{
    public class MyBrokerContext : BrokerContext
    {
        public Topic<OrderEvent> OrderEvents { get; set; }
        public Topic<CustomerEvent> CustomerEvents { get; set; }

        protected override void OnModelCreating(BrokerModelBuilder model)
        {
            model.Topic<OrderEvent>(topic =>
            {
                topic.HasName("order-events")
                     .HasSetting(x => x.RetentionMs = "5000");

                topic.HasProducer()// adding producer registers the ProduceAsync
                     .HasKey(x => x.OrderId)
                     .SetSerializer(x => x.UseJsonSerializer())
                     .Features
                        .AddOutbox(outbox => outbox.UseInMemory())
                        .AddRetry(retry => retry.WithIntervals(100, 500, 3000));

                topic.HasConsumer()// adding consumer register the GetConsumer
                     .GroupId("hello-world-consumer")
                     .SetDeserializer(x => x.UseJsonDeserializer())
                     .UseHandler<ConsoleHandler>();//adding handler do not register the GetConsumer extension
            });

            model.Topic<CustomerEvent>(topic =>
            {
                topic.HasName("customer-events").HasConsumer();
            });
        }

        protected override void OnConfiguring(BrokerOptionsBuilder options)
        {
            options.WithBootstrapServers("localhost:9092")
                   .WithClientId("HelloWorldClient");
        }
    }

    internal class ConsoleHandler
    {
        public static async ValueTask HandleAsync(OrderEvent message, ConsumeContext context, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Received Order Event: {message.OrderId} at {context.Timestamp}");

            await context.CommitAsync(cancellationToken);
        }
    }

    public class CustomerEvent
    {
        public string CustomerId { get; set; }
    }

    public class OrderEvent
    {
        public string OrderId { get; set; }
    }
}
