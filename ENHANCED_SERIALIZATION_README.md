# Enhanced Serialization Middleware System

## Overview

The enhanced serialization middleware system provides flexible, pluggable serialization support for multiple frameworks while maintaining clean separation of concerns. The system supports:

- **System.Text.Json** (default, built-in)
- **Newtonsoft.Json** (requires NuGet package)
- **MessagePack** (requires NuGet package)

## Architecture

### Core Components

1. **`SerializationOptions<T>`** - Configuration options that store serialization strategy and framework-specific settings
2. **`SerializationMiddleware<T>`** - Unified middleware that handles both serialization and deserialization
3. **`IMessageSerializer<T>`** - Strategy interface for different serialization implementations
4. **Strategy Implementations** - Framework-specific serializers that use reflection to avoid direct dependencies

### Key Design Principles

- **Single Middleware**: One `SerializationMiddleware<T>` handles both producer serialization and consumer deserialization
- **Strategy Pattern**: Framework-specific serializers implement `IMessageSerializer<T>` interface
- **Reflection-Based**: External dependencies (Newtonsoft.Json, MessagePack) are accessed via reflection to avoid hard dependencies
- **Type Safety**: Generic `<T>` ensures compile-time type validation
- **Auto-Enablement**: Calling any `UseXXX()` method automatically enables the middleware

## Configuration API

### System.Text.Json (Default)

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
});
```

### Newtonsoft.Json

```csharp
// Requires: Install-Package Newtonsoft.Json
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseNewtonsoftJson(settings =>
    {
        var jsonSettings = (JsonSerializerSettings)settings;
        jsonSettings.NullValueHandling = NullValueHandling.Ignore;
        jsonSettings.Formatting = Formatting.None;
        jsonSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
    });
});
```

### MessagePack

```csharp
// Requires: Install-Package MessagePack
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseMessagePack(options =>
    {
        var packOptions = (MessagePackSerializerOptions)options;
        // Configure MessagePack-specific options
    });
});
```

## Implementation Details

### SerializationOptions<T>

```csharp
public class SerializationOptions<T> : MiddlewareOptions<T>
{
    public SerializationStrategy Strategy { get; set; }
    public JsonSerializerOptions SystemTextJsonOptions { get; set; }
    public object? NewtonsoftJsonSettings { get; set; }
    public object? MessagePackOptions { get; set; }
}
```

### SerializationMiddleware<T>

- **Initialization**: Creates appropriate serializer based on configured strategy
- **Consumer Mode**: Deserializes `ISerializedEnvelope<T>.SerializedData` to `Envelope<T>.Message`
- **Producer Mode**: Prepares message for serialization (framework integration point)
- **Error Handling**: Throws descriptive exceptions for missing dependencies or configuration

### Strategy Implementations

Each serializer uses reflection to access external libraries:

```csharp
// NewtonsoftJsonSerializer<T>
var jsonConvertType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json");
var serializeMethod = jsonConvertType.GetMethod("SerializeObject", ...);

// MessagePackSerializer<T>  
var messagePackType = Type.GetType("MessagePack.MessagePackSerializer, MessagePack");
var serializeMethod = messagePackType.GetMethod("Serialize", ...);
```

## Pipeline Integration

### Consumer Pipeline
```
SerializationMiddleware → InboxMiddleware → RetryMiddleware → ...
```

### Producer Pipeline
```
SerializationMiddleware → OutboxMiddleware → RetryMiddleware → ...
```

## Usage Examples

### Mixed Serialization Strategies

```csharp
// High-performance events use MessagePack
modelBuilder.Topic<HighVolumeEvent>(topic =>
{
    topic.UseMessagePack();
    topic.HasProducer(producer => producer.HasOutbox());
});

// API events use JSON for readability
modelBuilder.Topic<ApiEvent>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.WriteIndented = true;
    });
});

// Legacy integration uses Newtonsoft.Json
modelBuilder.Topic<LegacyEvent>(topic =>
{
    topic.UseNewtonsoftJson(settings =>
    {
        var json = (JsonSerializerSettings)settings;
        json.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    });
});
```

## Migration from Legacy System

### Before (Legacy)
```csharp
topic.UseJsonSerializer(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
// Uses SerializationOptions<T>.Options (JsonSerializerOptions)
```

### After (Enhanced)
```csharp
topic.UseJsonSerializer(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
// Uses SerializationOptions<T>.SystemTextJsonOptions (JsonSerializerOptions)
```

**Breaking Changes:**
- `SerializationOptions<T>.Options` → `SerializationOptions<T>.SystemTextJsonOptions`
- `DeserializerMiddleware<T>` is obsolete, use `SerializationMiddleware<T>`

## Error Handling

### Missing Dependencies
```csharp
// If Newtonsoft.Json is not installed
InvalidOperationException: "Newtonsoft.Json is not available. Please install the Newtonsoft.Json NuGet package."

// If MessagePack is not installed  
InvalidOperationException: "MessagePack is not available. Please install the MessagePack NuGet package."
```

### Configuration Errors
```csharp
// If strategy is set but options are null
InvalidOperationException: "NewtonsoftJsonSettings must be configured when using NewtonsoftJson strategy."
```

## Performance Considerations

- **System.Text.Json**: Fastest, smallest memory footprint (default)
- **MessagePack**: Best compression ratio, fastest for binary data
- **Newtonsoft.Json**: Most compatible, slowest performance

Choose the appropriate serializer based on your specific requirements:
- **API Integration**: System.Text.Json
- **High-Volume Events**: MessagePack  
- **Legacy Compatibility**: Newtonsoft.Json

## Extensibility

To add support for additional serialization frameworks:

1. Create serializer implementation of `IMessageSerializer<T>`
2. Add strategy to `SerializationStrategy` enum
3. Add configuration method to `TopicTypeBuilder<T>`
4. Update `SerializationMiddleware<T>.CreateSerializer()` method

Example for adding Protocol Buffers support:

```csharp
public enum SerializationStrategy
{
    SystemTextJson,
    NewtonsoftJson,
    MessagePack,
    ProtocolBuffers // New strategy
}

public TopicTypeBuilder<T> UseProtocolBuffers(Action<object>? options = null)
{
    // Implementation similar to UseMessagePack
}
```
