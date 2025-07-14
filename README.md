# K-Entity-Framework

K-Entity-Framework is a lightweight .NET library for working with Apache Kafka. It provides a simple and efficient way to produce and consume messages in Kafka topics.

### Producer
Here is an example of how to create a producer and send messages to a Kafka topic:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBrokerContext("localhost:9092");

var app = builder.Build();

var kafka = app.Services.GetService<Kafka>();

await kafka.OrderEvents.ProduceAsync(new OrderPlaced());
```

### Consumer
Here is an example of how to create a consumer and read messages from a Kafka topic:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBrokerContext("localhost:9092");

var app = builder.Build();

var kafka = app.Services.GetService<Kafka>();

var result = await kafka.OrderEvents.Consumer.ConsumeAsync();

await kafka.OrderEvents.Consumer.CommitAsync();
```