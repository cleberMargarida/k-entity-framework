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

Implement `IMessageSerializer<T>` and `IMessageDeserializer<T>`:

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