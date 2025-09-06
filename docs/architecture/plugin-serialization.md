# Plugin-Based Serialization Architecture

K-Entity-Framework features a sophisticated plugin-based serialization system that allows external libraries to register their own serialization strategies without modifying the core framework. This provides true extensibility where new serializers can be added as separate NuGet packages or in-app implementations.

## Key Benefits

✅ **True Extensibility**: Add new serializers without touching core code  
✅ **No Dependencies**: Core framework only includes System.Text.Json  
✅ **Plugin Architecture**: External strategies via separate packages  
✅ **Type Safety**: Compile-time validation through generics  
✅ **Unified API**: All serializers use the same `UseSerializer()` method  
✅ **Convenience Methods**: Optional type-safe extension methods per strategy  
✅ **Polymorphic Support**: Automatic handling of inheritance hierarchies and derived types  

## Architecture Components

### 1. Core Interfaces

```csharp
public interface IMessageSerializer<T> where T : class
{
    byte[] Serialize(T message);
    T? Deserialize(byte[] data);
}
```

The core serialization interface is simple and generic, allowing any serialization library to implement message serialization and deserialization.

### 2. Strategy Registry

```csharp
public static class SerializationStrategyRegistry
{
    private static readonly ConcurrentDictionary<string, SerializationStrategyDescriptor> _strategies = new();
    
    public static void RegisterStrategy(SerializationStrategyDescriptor descriptor)
    {
        _strategies.TryAdd(descriptor.Name, descriptor);
    }
    
    public static IMessageSerializer<T> CreateSerializer<T>(string strategyName, object options) where T : class
    {
        if (_strategies.TryGetValue(strategyName, out var descriptor))
        {
            return descriptor.CreateSerializer<T>(options);
        }
        
        throw new InvalidOperationException($"Serialization strategy '{strategyName}' not found");
    }
    
    public static object CreateDefaultOptions(string strategyName)
    {
        if (_strategies.TryGetValue(strategyName, out var descriptor))
        {
            return descriptor.CreateDefaultOptions();
        }
        
        throw new InvalidOperationException($"Serialization strategy '{strategyName}' not found");
    }
}
```

### 3. Strategy Descriptor

```csharp
public class SerializationStrategyDescriptor
{
    public string Name { get; }
    public string Description { get; }
    public Func<Type, object, object> SerializerFactory { get; }
    public Func<object> DefaultOptionsFactory { get; }
    
    public SerializationStrategyDescriptor(
        string name,
        string description,
        Func<Type, object, object> serializerFactory,
        Func<object> defaultOptionsFactory)
    {
        Name = name;
        Description = description;
        SerializerFactory = serializerFactory;
        DefaultOptionsFactory = defaultOptionsFactory;
    }
    
    public IMessageSerializer<T> CreateSerializer<T>(object options) where T : class
    {
        return (IMessageSerializer<T>)SerializerFactory(typeof(T), options);
    }
    
    public object CreateDefaultOptions() => DefaultOptionsFactory();
}
```

### 4. Plugin Registration

```csharp
public static class SerializationExtensions
{
    public static void RegisterStrategy<TSerializer, TOptions>(
        string name, 
        Func<TOptions> defaultFactory,
        string description = null)
        where TSerializer : class
        where TOptions : class
    {
        var descriptor = new SerializationStrategyDescriptor(
            name,
            description ?? $"{name} serialization strategy",
            (type, options) => CreateSerializerInstance<TSerializer>(type, (TOptions)options),
            () => defaultFactory()
        );
        
        SerializationStrategyRegistry.RegisterStrategy(descriptor);
    }
    
    public static void RegisterCustomStrategy(
        string name, 
        Func<Type, object, object> factory, 
        Func<object> defaultFactory,
        string description = null)
    {
        var descriptor = new SerializationStrategyDescriptor(
            name,
            description ?? $"{name} serialization strategy",
            factory,
            defaultFactory
        );
        
        SerializationStrategyRegistry.RegisterStrategy(descriptor);
    }
    
    private static object CreateSerializerInstance<TSerializer>(Type messageType, object options)
    {
        var serializerType = typeof(TSerializer).MakeGenericType(messageType);
        return Activator.CreateInstance(serializerType, options);
    }
}
```

## Usage Patterns

### Built-in System.Text.Json (Default)

The framework includes System.Text.Json as the default serializer:

```csharp
modelBuilder.Topic<Order>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
});
```

### Generic Strategy Usage

Any registered strategy can be used via the generic `UseSerializer` method:

```csharp
modelBuilder.Topic<Order>(topic =>
{
    topic.UseSerializer("NewtonsoftJson", options =>
    {
        var settings = (JsonSerializerSettings)options;
        settings.DateFormatString = "yyyy-MM-dd";
        settings.NullValueHandling = NullValueHandling.Ignore;
    });
});
```

### Type-Safe Extensions (Plugin-Provided)

Plugin packages can provide strongly-typed extension methods:

```csharp
modelBuilder.Topic<Order>(topic =>
{
    topic.UseNewtonsoftJson(settings =>
    {
        settings.DateFormatString = "yyyy-MM-dd";
        settings.NullValueHandling = NullValueHandling.Ignore;
        settings.Formatting = Formatting.Indented;
    });
});
```

## Creating External Strategy Plugins

### Step 1: Create Serializer Implementation

Create a new class library project for your serialization plugin:

```csharp
// In external package: K.EntityFrameworkCore.Serialization.NewtonsoftJson
using Newtonsoft.Json;

public class NewtonsoftJsonSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public byte[] Serialize(T message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        
        var json = JsonConvert.SerializeObject(message, _settings);
        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        
        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json, _settings);
    }
}
```

### Step 2: Create Plugin Registration

```csharp
public static class NewtonsoftJsonPlugin
{
    private static bool _isRegistered = false;
    
    public static void Register()
    {
        if (_isRegistered) return;
        
        SerializationExtensions.RegisterStrategy<NewtonsoftJsonSerializer<>, JsonSerializerSettings>(
            "NewtonsoftJson",
            () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DefaultValueHandling = DefaultValueHandling.Include
            },
            "Newtonsoft.Json serialization strategy with high performance and flexibility"
        );
        
        _isRegistered = true;
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
        // Ensure the plugin is registered
        NewtonsoftJsonPlugin.Register();
        
        return builder.UseSerializer("NewtonsoftJson", options =>
        {
            if (options is JsonSerializerSettings settings)
            {
                configure?.Invoke(settings);
            }
        });
    }
}
```

### Step 4: Package Configuration

Create a `PackageReference` in your plugin project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <PackageId>K.EntityFrameworkCore.Serialization.NewtonsoftJson</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>Newtonsoft.Json serialization plugin for K-Entity-Framework</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="K.EntityFrameworkCore" Version="[core-version]" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
```

## Polymorphic Serialization Support

The plugin-based serialization architecture fully supports polymorphic message serialization. The framework automatically manages type information through message headers, enabling seamless serialization and deserialization of derived types.

### Core Mechanisms

All serialization strategies leverage the same polymorphic infrastructure:

1. **Type Header Management**: The framework automatically adds type information to message headers
2. **Runtime Type Resolution**: Uses `$runtimeType` or `$type` headers to determine the correct type during deserialization
3. **Strategy-Agnostic**: Works with any serialization plugin (System.Text.Json, Newtonsoft.Json, MessagePack, etc.)

### Framework Integration

The `SystemTextJsonSerializer<T>` implementation demonstrates the pattern:

```csharp
internal class SystemTextJsonSerializer<T> : IMessageSerializer<T, JsonSerializerOptions>
{
    public ReadOnlySpan<byte> Serialize(IImmutableDictionary<string, string> headers, in T message)
    {
        // Uses the runtime type of the message for serialization
        Type type = message.GetType();
        return JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
    }

    public T Deserialize(IImmutableDictionary<string, string> headers, ReadOnlySpan<byte> data)
    {
        // Resolves the type from headers
        Type type = GetType(headers);
        return JsonSerializer.Deserialize(data, type, Options) as T;
    }

    private static Type GetType(IImmutableDictionary<string, string> headers)
    {
        // Framework manages these headers automatically
        var assemblyQualifiedName = headers.GetValueOrDefault("$runtimeType") ?? headers["$type"];
        return Type.GetType(assemblyQualifiedName, throwOnError: true)!;
    }
}
```

### Plugin Implementation Guidelines

When implementing polymorphic-aware plugins:

#### 1. Always Use Runtime Type for Serialization

```csharp
public class CustomSerializer<T> : IMessageSerializer<T> where T : class
{
    public ReadOnlySpan<byte> Serialize(IImmutableDictionary<string, string> headers, in T message)
    {
        // ✅ Use actual runtime type, not generic parameter T
        Type runtimeType = message.GetType();
        return SerializeWithType(message, runtimeType);
    }
}
```

#### 2. Resolve Type from Headers for Deserialization

```csharp
public T Deserialize(IImmutableDictionary<string, string> headers, ReadOnlySpan<byte> data)
{
    // ✅ Always check headers for type information
    Type targetType = ResolveTypeFromHeaders(headers);
    return DeserializeWithType(data, targetType) as T;
}

private Type ResolveTypeFromHeaders(IImmutableDictionary<string, string> headers)
{
    // Follow framework convention
    var typeName = headers.GetValueOrDefault("$runtimeType") ?? 
                   headers.GetValueOrDefault("$type") ??
                   typeof(T).AssemblyQualifiedName;
    
    return Type.GetType(typeName, throwOnError: true)!;
}
```

### Serialization Strategy Considerations

Different serialization libraries handle polymorphism differently:

#### System.Text.Json
- Built-in support via `JsonDerivedType` attributes (optional)
- Runtime type handling works automatically
- Consider `JsonTypeInfoResolver` for advanced scenarios

#### Newtonsoft.Json
- Natural support for polymorphic serialization
- `TypeNameHandling` settings can complement framework headers
- Built-in `$type` metadata works alongside framework headers

#### MessagePack
- Requires explicit type mapping configuration
- Use framework headers as the source of truth for type resolution
- Consider `MessagePackObjectAttribute` for known hierarchies

#### Custom Binary Serializers
- Must implement type resolution logic manually
- Should write type identifiers to the payload or rely solely on headers
- Framework headers provide consistent type information

### Best Practices

1. **Header Priority**: Always prioritize `$runtimeType` over `$type` for maximum compatibility
2. **Type Safety**: Include proper error handling for unknown or invalid types
3. **Version Compatibility**: Consider how type changes affect deserialization across plugin versions
4. **Performance**: Cache type resolution results when possible

### Testing Polymorphic Scenarios

Ensure your plugin handles polymorphic serialization correctly:

```csharp
[Test]
public void Should_SerializeAndDeserialize_PolymorphicTypes()
{
    // Arrange
    var baseMessage = new BaseMessage(1, "Base");
    var derivedMessage = new DerivedMessage(2, "Derived", "ExtraData");
    
    // Act & Assert - Test both directions
    TestRoundTrip(baseMessage);
    TestRoundTrip<BaseMessage>(derivedMessage); // Serialize as derived, deserialize as base
}
```

## Advanced Plugin Examples

### MessagePack Plugin

```csharp
public class MessagePackSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly MessagePackSerializerOptions _options;

    public MessagePackSerializer(MessagePackSerializerOptions options)
    {
        _options = options;
    }

    public byte[] Serialize(T message)
    {
        return MessagePack.MessagePackSerializer.Serialize(message, _options);
    }

    public T? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        return MessagePack.MessagePackSerializer.Deserialize<T>(data, _options);
    }
}

public static class MessagePackPlugin
{
    public static void Register()
    {
        SerializationExtensions.RegisterStrategy<MessagePackSerializer<>, MessagePackSerializerOptions>(
            "MessagePack",
            () => MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray),
            "MessagePack binary serialization with optional compression"
        );
    }
}
```

### Protobuf Plugin

```csharp
public class ProtobufSerializer<T> : IMessageSerializer<T> where T : class, IMessage<T>, new()
{
    private readonly JsonFormatter _formatter;
    
    public ProtobufSerializer(JsonFormatter formatter)
    {
        _formatter = formatter;
    }

    public byte[] Serialize(T message)
    {
        return message.ToByteArray();
    }

    public T? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        
        var parser = new MessageParser<T>(() => new T());
        return parser.ParseFrom(data);
    }
}
```

### Custom Binary Plugin

```csharp
public class CustomBinarySerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly BinarySerializationOptions _options;

    public CustomBinarySerializer(BinarySerializationOptions options)
    {
        _options = options;
    }

    public byte[] Serialize(T message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // Custom binary serialization logic
        SerializeObject(writer, message, _options);
        
        return stream.ToArray();
    }

    public T? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        
        // Custom binary deserialization logic
        return DeserializeObject<T>(reader, _options);
    }
}
```

## Plugin Discovery

### Automatic Registration

Plugins can use module initializers for automatic registration:

```csharp
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        NewtonsoftJsonPlugin.Register();
    }
}
```

### Manual Registration

Applications can manually register plugins during startup:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register serialization plugins
        NewtonsoftJsonPlugin.Register();
        MessagePackPlugin.Register();
        ProtobufPlugin.Register();
        
        // Configure EF with Kafka
        services.AddDbContext<MyContext>(options =>
            options.UseSqlServer(connectionString)
                   .UseKafkaExtensibility(kafka => { /* config */ }));
    }
}
```

## Performance Considerations

### 1. Serializer Caching

Serializers are cached per message type for optimal performance:

```csharp
private static readonly ConcurrentDictionary<Type, IMessageSerializer<object>> _serializerCache = new();

public static IMessageSerializer<T> GetCachedSerializer<T>(string strategy, object options) where T : class
{
    var key = typeof(T);
    return (IMessageSerializer<T>)_serializerCache.GetOrAdd(key, _ => 
        SerializationStrategyRegistry.CreateSerializer<T>(strategy, options));
}
```

### 2. Options Validation

Validate options at registration time, not at runtime:

```csharp
public static void RegisterStrategy<TOptions>(string name, Func<TOptions> defaultFactory)
{
    // Validate default options at registration time
    var defaultOptions = defaultFactory();
    if (defaultOptions == null)
        throw new ArgumentException("Default options factory returned null");
    
    // Register with validated factory
    // ...
}
```

### 3. Reflection Optimization

Use compiled expressions for better performance:

```csharp
private static readonly ConcurrentDictionary<Type, Func<object, object>> _constructorCache = new();

private static object CreateSerializerInstance<TSerializer>(Type messageType, object options)
{
    var serializerType = typeof(TSerializer).MakeGenericType(messageType);
    
    var factory = _constructorCache.GetOrAdd(serializerType, type =>
    {
        var constructor = type.GetConstructor(new[] { options.GetType() });
        var param = Expression.Parameter(typeof(object));
        var newExp = Expression.New(constructor, Expression.Convert(param, options.GetType()));
        return Expression.Lambda<Func<object, object>>(newExp, param).Compile();
    });
    
    return factory(options);
}
```

## Testing Strategies

### Unit Testing Plugins

```csharp
[Test]
public void NewtonsoftJsonSerializer_SerializesCorrectly()
{
    // Arrange
    var settings = new JsonSerializerSettings();
    var serializer = new NewtonsoftJsonSerializer<TestMessage>(settings);
    var message = new TestMessage { Id = 123, Name = "Test" };
    
    // Act
    var serialized = serializer.Serialize(message);
    var deserialized = serializer.Deserialize(serialized);
    
    // Assert
    Assert.Equal(message.Id, deserialized.Id);
    Assert.Equal(message.Name, deserialized.Name);
}
```

### Integration Testing

```csharp
[Test]
public void Plugin_IntegratesWithFramework()
{
    // Arrange
    NewtonsoftJsonPlugin.Register();
    
    var builder = new TopicTypeBuilder<TestMessage>();
    
    // Act & Assert
    Assert.DoesNotThrow(() =>
    {
        builder.UseNewtonsoftJson(settings =>
        {
            settings.NullValueHandling = NullValueHandling.Ignore;
        });
    });
}
```

## Best Practices

### 1. Plugin Naming

- Use descriptive names: `"NewtonsoftJson"`, `"MessagePack"`, `"Protobuf"`
- Avoid version numbers in names unless necessary
- Use consistent casing across all plugins

### 2. Error Handling

- Provide clear error messages for unsupported types
- Handle null inputs gracefully
- Include serialization context in exceptions

### 3. Thread Safety

- Ensure serializers are thread-safe
- Use immutable options where possible
- Cache expensive operations safely

### 4. Documentation

- Provide clear usage examples
- Document performance characteristics
- Include migration guides for existing serializers

## Next Steps

- [Serialization Features](../features/serialization.md) - Learn about available serialization options including polymorphic message support
- [Middleware Architecture](middleware-architecture.md) - Understand how serialization fits into the middleware pipeline
- [Performance Tuning](../guides/kafka-configuration.md) - Optimize serialization performance
