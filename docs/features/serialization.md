# Serialization

K-Entity-Framework uses `System.Text.Json` by default, but you can use custom serializers.

## Default Configuration

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseSystemTextJson(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false;
    });
});
```

## Custom Serializers

Implement [`IMessageSerializer<T>`](../api/K.EntityFrameworkCore.Interfaces.IMessageSerializer-2.yml) and [`IMessageDeserializer<T>`](../api/K.EntityFrameworkCore.Interfaces.IMessageDeserializer-2.yml):

> [!TIP]
> You can register a custom serializer for a topic using `topic.UseSerializer<TSerializer, TSettings>(...)`. Provide any serializer-specific settings via the factory argument so the framework constructs and configures the serializer for you.

```csharp
public class NewtonsoftJsonSerializer<T> : IMessageSerializer<T, JsonSerializerSettings>, 
    IMessageDeserializer<T> where T : class
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializer(JsonSerializerSettings settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings();
    }

    public byte[] Serialize(in T message)
    {
        var json = JsonConvert.SerializeObject(message, _settings);
        return Encoding.UTF8.GetBytes(json);
    }

    public T Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json, _settings);
    }
}
```

Use it:
```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseSerializer<NewtonsoftJsonSerializer<OrderCreated>, JsonSerializerSettings>(settings =>
    {
        settings.NullValueHandling = NullValueHandling.Ignore;
    });
});
```

## Polymorphic Messages

The framework automatically handles inheritance hierarchies.

### Setup
```csharp
// Base type
public record OrderEvent(int OrderId, DateTime Timestamp);

// Derived types
public record OrderCreated(int OrderId, DateTime Timestamp, string CustomerEmail) 
    : OrderEvent(OrderId, Timestamp);

public record OrderShipped(int OrderId, DateTime Timestamp, string TrackingNumber) 
    : OrderEvent(OrderId, Timestamp);
```

### Configuration
Configure once using the base type:
```csharp
modelBuilder.Topic<OrderEvent>(topic =>
{
    topic.HasName("order-events");
    topic.UseSystemTextJson();
});
```

### Usage
```csharp
// Produce any derived type
context.OrderEvents.Produce(new OrderCreated(1, DateTime.Now, "user@example.com"));
context.OrderEvents.Produce(new OrderShipped(2, DateTime.Now, "TRACK123"));
await context.SaveChangesAsync();

// Consume with type checking
var message = await context.OrderEvents.FirstAsync();
switch (message)
{
    case OrderCreated created:
        Console.WriteLine($"Created for {created.CustomerEmail}");
        break;
    case OrderShipped shipped:
        Console.WriteLine($"Shipped: {shipped.TrackingNumber}");
        break;
}
```

## How It Works

- Type information stored in `$runtimeType` header
- No special configuration needed
- Works across different applications
- Unknown derived types fall back to base type

## Mixed Serializers

Use different serializers per topic:

```csharp
modelBuilder.Topic<ApiEvent>(topic => topic.UseSystemTextJson());
modelBuilder.Topic<LegacyEvent>(topic => topic.UseSerializer<CustomSerializer<LegacyEvent>>());
```

> [!TIP]
> K-Entity-Framework is built to push a lot of messages fast while keeping memory use low.
> </br>
> It leans on [`Span<T>`](https://learn.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay#what-is-spant) everywhere – working directly on memory with almost no copying.
> </br></br>
> The core [`Envelope<T>`](../api/K.EntityFrameworkCore.Envelope-1.yml) `ref struct` encapsulates messages, headers, and serialized payloads as [`ReadOnlySpan<byte>`](https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1?view=net-9.0), enabling zero-allocation middleware pipelines and direct memory access throughout the serialization process.