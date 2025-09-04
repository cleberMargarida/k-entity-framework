# Serialization

K-Entity-Framework provides an extensible serialization system. The default serializer is `System.Text.Json`, but you can plug in your own custom serializers.

## `System.Text.Json` (Default)

The framework uses `System.Text.Json` by default. You can customize the `JsonSerializerOptions` for each topic.

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

You can implement your own serializer by creating a class that implements `IMessageSerializer<T>` and `IMessageDeserializer<T>`. 

### Example: `Newtonsoft.Json` Serializer

1.  **Implement the serializer:**

```csharp
public class NewtonsoftJsonSerializer<T> : IMessageSerializer<T, JsonSerializerSettings>, IMessageDeserializer<T>
    where T : class
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializer(JsonSerializerSettings settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings();
    }

    public byte[] Serialize(in T message)
    {
        var json = JsonConvert.SerializeObject(message, _settings);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public T Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json, _settings);
    }
}
```

2.  **Use it in your configuration:**

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseSerializer<NewtonsoftJsonSerializer<OrderCreated>, JsonSerializerSettings>(settings =>
    {
        settings.NullValueHandling = NullValueHandling.Ignore;
        settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });
});
```

## Mixed Serialization Strategies

You can use different serializers for different topics in the same application.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Use System.Text.Json for this topic
    modelBuilder.Topic<ApiEvent>(topic =>
    {
        topic.HasName("api-events");
        topic.UseSystemTextJson(options =>
        {
            options.WriteIndented = true;
        });
    });

    // Use a custom serializer for this topic
    modelBuilder.Topic<LegacyEvent>(topic =>
    {
        topic.HasName("legacy-events");
        topic.UseSerializer<NewtonsoftJsonSerializer<LegacyEvent>, JsonSerializerSettings>();
    });
}
```