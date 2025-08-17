# Plugin-Based Serialization Architecture

## Overview

The new plugin-based serialization system allows external libraries to register their own serialization strategies without modifying the core framework. This provides true extensibility where new serializers can be added as separate NuGet packages or in-app implementations.

## Key Benefits

✅ **True Extensibility**: Add new serializers without touching core code  
✅ **No Dependencies**: Core framework only includes System.Text.Json  
✅ **Plugin Architecture**: External strategies via separate packages  
✅ **Type Safety**: Compile-time validation through generics  
✅ **Unified API**: All serializers use the same `UseSerializer()` method  
✅ **Convenience Methods**: Optional type-safe extension methods per strategy  

## Architecture Components

### 1. Core Interfaces

```csharp
public interface IMessageSerializer<T> where T : class
{
    byte[] Serialize(T message);
    T? Deserialize(byte[] data);
}
```

### 2. Strategy Registry

```csharp
public static class SerializationStrategyRegistry
{
    public static void RegisterStrategy(SerializationStrategyDescriptor descriptor);
    public static IMessageSerializer<T> CreateSerializer<T>(string strategyName, object options);
    public static object CreateDefaultOptions(string strategyName);
}
```

### 3. Plugin Registration

```csharp
public static class SerializationExtensions
{
    public static void RegisterStrategy<TSerializer, TOptions>(string name, Func<TOptions> defaultFactory);
    public static void RegisterCustomStrategy(string name, Func<Type, object, object> factory, Func<object> defaultFactory);
}
```

## Usage Patterns

### Built-in System.Text.Json (Default)

```csharp
modelBuilder.Topic<Order>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false;
    });
});
```

### Generic Strategy Usage

```csharp
modelBuilder.Topic<Order>(topic =>
{
    topic.UseSerializer("NewtonsoftJson", options =>
    {
        var settings = (JsonSerializerSettings)options;
        settings.DateFormatString = "yyyy-MM-dd";
    });
});
```

### With Type-Safe Extensions (when provided by plugin)

```csharp
modelBuilder.Topic<Order>(topic =>
{
    topic.UseNewtonsoftJson(settings =>
    {
        settings.DateFormatString = "yyyy-MM-dd";
        settings.NullValueHandling = NullValueHandling.Ignore;
    });
});
```

## Creating External Strategy Plugins

### Step 1: Create Serializer Implementation

```csharp
// In external package: K.EntityFrameworkCore.Serialization.NewtonsoftJson
public class NewtonsoftJsonSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
    {
        _settings = settings;
    }

    public byte[] Serialize(T message)
    {
        var json = JsonConvert.SerializeObject(message, _settings);
        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json, _settings);
    }
}
```

### Step 2: Create Plugin Registration

```csharp
public static class NewtonsoftJsonPlugin
{
    public static void Register()
    {
        SerializationExtensions.RegisterStrategy<NewtonsoftJsonSerializer<>, JsonSerializerSettings>(
            "NewtonsoftJson",
            () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            },
            "Newtonsoft.Json serialization strategy"
        );
    }
}
```

### Step 3: Create Convenience Extensions (Optional)

```csharp
public static class TopicTypeBuilderExtensions
{
    public static TopicTypeBuilder<T> UseNewtonsoftJson<T>(
        this TopicTypeBuilder<T> builder, 
        Action<JsonSerializerSettings>? configure = null) where T : class
    {
        return builder.UseSerializer("NewtonsoftJson", options =>
        {
            if (options is JsonSerializerSettings settings && configure != null)
            {
                configure(settings);
            }
        });
    }
}
```

### Step 4: Register in Application Startup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register the external strategy
    NewtonsoftJsonPlugin.Register();
    
    // Configure Entity Framework with Kafka
    services.AddDbContext<MyDbContext>(options =>
    {
        options.UseKafka(kafka => { /* kafka config */ });
    });
}
```

## Package Structure Examples

### Core Package
```
K.EntityFrameworkCore/
├── Interfaces/
│   └── IMessageSerializer.cs
├── Serialization/
│   ├── SerializationStrategyDescriptor.cs
│   ├── SerializationStrategyRegistry.cs
│   └── SystemTextJsonSerializer.cs
└── Extensions/
    └── SerializationExtensions.cs
```

### External Strategy Packages
```
K.EntityFrameworkCore.Serialization.NewtonsoftJson/
├── NewtonsoftJsonSerializer.cs
├── NewtonsoftJsonPlugin.cs
└── TopicTypeBuilderExtensions.cs

K.EntityFrameworkCore.Serialization.MessagePack/
├── MessagePackSerializer.cs
├── MessagePackPlugin.cs
└── TopicTypeBuilderExtensions.cs

K.EntityFrameworkCore.Serialization.Protobuf/
├── ProtobufSerializer.cs
├── ProtobufPlugin.cs
└── TopicTypeBuilderExtensions.cs
```

## Advanced Scenarios

### Custom Factory Logic

```csharp
SerializationExtensions.RegisterCustomStrategy(
    "Advanced",
    (messageType, options) => {
        // Complex creation logic based on message type
        if (messageType.HasAttribute<SpecialAttribute>())
        {
            return new SpecialSerializer(messageType, options);
        }
        return new StandardSerializer(messageType, options);
    },
    () => new AdvancedOptions(),
    "Advanced serialization with type-based logic"
);
```

### Conditional Strategy Selection

```csharp
public void ConfigureTopics(ModelBuilder modelBuilder)
{
    // Performance-critical events
    modelBuilder.Topic<HighVolumeEvent>(topic => topic.UseMessagePack());
    
    // API integration (human-readable)
    modelBuilder.Topic<ApiEvent>(topic => topic.UseJsonSerializer());
    
    // Legacy system compatibility
    modelBuilder.Topic<LegacyEvent>(topic => topic.UseNewtonsoftJson());
    
    // Custom binary protocol
    modelBuilder.Topic<BinaryEvent>(topic => topic.UseSerializer("CustomBinary"));
}
```

### Runtime Strategy Discovery

```csharp
public void ShowAvailableStrategies()
{
    var strategies = SerializationStrategyRegistry.GetAllStrategies();
    
    foreach (var strategy in strategies)
    {
        Console.WriteLine($"Strategy: {strategy.Name}");
        Console.WriteLine($"Description: {strategy.Description}");
        Console.WriteLine();
    }
}
```

## Migration from Legacy System

### Before (Fixed Strategies)
```csharp
// Limited to built-in strategies only
topic.UseJsonSerializer();
topic.UseNewtonsoftJson();  // Hardcoded in core
topic.UseMessagePack();     // Hardcoded in core
```

### After (Plugin-Based)
```csharp
// Built-in default
topic.UseJsonSerializer();

// External plugins (registered separately)
topic.UseSerializer("NewtonsoftJson");
topic.UseSerializer("MessagePack");
topic.UseSerializer("Protobuf");
topic.UseSerializer("CustomBinary");

// Or with type-safe extensions (if provided)
topic.UseNewtonsoftJson();
topic.UseMessagePack();
topic.UseProtobuf();
```

## Benefits for Ecosystem

### For Framework Maintainers
- **No Core Changes**: New serializers don't require core framework updates
- **Reduced Dependencies**: Core only includes essential System.Text.Json
- **Cleaner Architecture**: Plugin system separates concerns properly
- **Better Testing**: Each strategy can be tested independently

### For Plugin Authors  
- **Independent Development**: Create and maintain serializers separately
- **Flexible Versioning**: Update plugins without waiting for core releases
- **Targeted Dependencies**: Only include what's needed for specific strategy
- **Custom Logic**: Full control over serialization behavior

### For End Users
- **Choice**: Use only the serializers they need
- **Performance**: No overhead from unused strategies
- **Consistency**: Same API for all serializers
- **Extensibility**: Easy to add custom serializers for specific needs

This plugin-based architecture provides true extensibility while maintaining clean separation of concerns and allowing the ecosystem to grow organically without core framework modifications.
