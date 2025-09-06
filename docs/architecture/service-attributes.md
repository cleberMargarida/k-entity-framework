# Service Attributes and Lifetime Management

K-Entity-Framework uses a comprehensive service attribute system to clearly document and manage the lifetime of services within the dependency injection container. This system enhances debugging capabilities and provides clear documentation of service registration patterns.

## Overview

The framework decorates all services with lifetime attributes to make service registration and lifetime management explicit and debuggable. This approach helps developers understand the service lifecycle and identify potential issues during development.

## Service Lifetime Attributes

### ScopedService Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ScopedServiceAttribute : Attribute
{
    public string Description { get; }
    
    public ScopedServiceAttribute(string description = null)
    {
        Description = description;
    }
}
```

### SingletonService Attribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class SingletonServiceAttribute : Attribute
{
    public string Description { get; }
    
    public SingletonServiceAttribute(string description = null)
    {
        Description = description;
    }
}
```

## Service Classification

### Scoped Services

Scoped services are created once per scope (typically per HTTP request or per message processing cycle). These services can maintain state during the scope but are disposed when the scope ends.

#### Middleware Invokers

```csharp
[ScopedService("Manages the execution pipeline for consumer middleware")]
public class ConsumerMiddlewareInvoker<T> : IMiddlewareInvoker<T>
{
    // New instance per message processing cycle
}

[ScopedService("Manages the execution pipeline for producer middleware")]
public class ProducerMiddlewareInvoker<T> : IMiddlewareInvoker<T>
{
    // New instance per message producing cycle
}
```

#### Middleware Components

```csharp
[ScopedService("Handles message deduplication and persistence")]
public class InboxMiddleware<T> : IMiddleware<T>
{
    private readonly IDbContext _dbContext; // Scoped dependency
}

[ScopedService("Manages message subscription and consumption")]
public class SubscriptionMiddleware<T> : IMiddleware<T>
{
    // Stateful during message processing
}

[ScopedService("Handles message serialization for producers")]
public class SerializerMiddleware<T> : IMiddleware<T>
{
    // Per-operation serialization context
}

[ScopedService("Handles message deserialization for consumers")]
public class DeserializerMiddleware<T> : IMiddleware<T>
{
    // Per-operation deserialization context
}
```

#### Command Registry

```csharp
[ScopedService("Registry for scoped command handlers")]
public class ScopedCommandRegistry
{
    // Maintains command state during request scope
}
```

### Singleton Services

Singleton services are created once and shared across the entire application lifetime. These services should be thread-safe and stateless or contain only immutable state.

#### Settings and Configuration

```csharp
[SingletonService("Configuration settings for serialization middleware")]
public class SerializationMiddlewareSettings<T> : MiddlewareSettings<T>
{
    // Immutable configuration
}

[SingletonService("Client configuration settings")]
public class ClientSettings<T>
{
    // Thread-safe configuration
}

[SingletonService("Configuration for inbox deduplication")]
public class InboxMiddlewareSettings<T> : MiddlewareSettings<T>
{
    // Immutable middleware settings
}
```

#### Registries and Managers

```csharp
[SingletonService("Global registry for subscription management")]
public class SubscriptionRegistry<T>
{
    private readonly ConcurrentDictionary<string, ISubscription> _subscriptions;
    // Thread-safe subscription tracking
}

[SingletonService("Manages Kafka consumer polling operations")]
public class KafkaConsumerPollService
{
    // Long-lived service for polling coordination
}

[SingletonService("Coordinates polling operations across consumers")]
public class PollerManager
{
    // Global coordination of polling activities
}
```

#### Middleware Settings

```csharp
[SingletonService("Producer middleware configuration")]
public class ProducerMiddlewareSettings<T> : MiddlewareSettings<T>
{
    // Immutable producer configuration
}

[SingletonService("Consumer middleware configuration")]
public class ConsumerMiddlewareSettings<T> : MiddlewareSettings<T>
{
    // Immutable consumer configuration
}

[SingletonService("Outbox pattern configuration")]
public class OutboxMiddlewareSettings<T> : MiddlewareSettings<T>
{
    // Transactional outbox settings
}
```

## Registration Patterns

### Automatic Registration

The framework automatically registers services based on their attributes during startup:

```csharp
public class KafkaOptionsExtension : IDbContextOptionsExtension
{
    public void ApplyServices(IServiceCollection services)
    {
        // Scoped services
        services.AddScoped<ScopedCommandRegistry>();
        services.AddScoped<ConsumerMiddlewareInvoker<>>();
        services.AddScoped<ProducerMiddlewareInvoker<>>();
        services.AddScoped<InboxMiddleware<>>();
        
        // Singleton services
        services.AddSingleton<SerializationMiddlewareSettings<>>();
        services.AddSingleton<KafkaConsumerPollService>();
        services.AddSingleton<PollerManager>();
        
        // Generic registrations for all message types
        RegisterGenericServices(services);
    }
    
    private void RegisterGenericServices(IServiceCollection services)
    {
        // Register middleware settings as singletons
        services.AddSingleton(typeof(InboxMiddlewareSettings<>));
        services.AddSingleton(typeof(OutboxMiddlewareSettings<>));
        
        // Register middleware instances as scoped
        services.AddScoped(typeof(InboxMiddleware<>));
        services.AddScoped(typeof(OutboxMiddleware<>));
    }
}
```

### Type-Specific Registration

For message-type-specific services, the framework uses generic type constraints:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessageTypeServices<T>(this IServiceCollection services)
        where T : class
    {
        // Register settings as singleton for this specific type
        services.AddSingleton<SerializationMiddlewareSettings<T>>();
        services.AddSingleton<InboxMiddlewareSettings<T>>();
        
        // Register middleware as scoped for this specific type
        services.AddScoped<InboxMiddleware<T>>();
        services.AddScoped<SerializerMiddleware<T>>();
        
        return services;
    }
}
```

## Service Lifetime Best Practices

### 1. Scoped Service Guidelines

**Do:**
- Use scoped lifetime for services that maintain state during a request/operation
- Access database contexts and other scoped dependencies
- Implement `IDisposable` if managing resources

**Don't:**
- Share state between different scopes
- Hold references to singleton services that might capture scope state
- Assume services will be disposed immediately

```csharp
[ScopedService]
public class MessageProcessor<T>
{
    private readonly IDbContext _context; // Scoped dependency - OK
    private readonly ILogger<MessageProcessor<T>> _logger; // Can be singleton or scoped
    
    public async Task ProcessAsync(T message)
    {
        // Safe to maintain state during this operation
        var processingState = new ProcessingState();
        
        // Use scoped database context
        await _context.SaveChangesAsync();
    }
}
```

### 2. Singleton Service Guidelines

**Do:**
- Keep services stateless or use only immutable state
- Ensure thread safety for all operations
- Use concurrent collections for mutable state

**Don't:**
- Store request-specific or user-specific data
- Capture scoped services in constructor
- Assume single-threaded access

```csharp
[SingletonService]
public class MessageTypeRegistry
{
    private readonly ConcurrentDictionary<Type, MessageTypeDescriptor> _types = new();
    
    public void RegisterType<T>(MessageTypeDescriptor descriptor)
    {
        // Thread-safe registration
        _types.TryAdd(typeof(T), descriptor);
    }
    
    public MessageTypeDescriptor GetDescriptor<T>()
    {
        // Thread-safe retrieval
        return _types.TryGetValue(typeof(T), out var descriptor) ? descriptor : null;
    }
}
```

## Debugging and Diagnostics

### Service Lifetime Validation

The framework provides tools to validate service lifetime configurations:

```csharp
public static class ServiceLifetimeValidator
{
    public static void ValidateRegistrations(IServiceCollection services)
    {
        foreach (var service in services)
        {
            var serviceType = service.ServiceType;
            var implementationType = service.ImplementationType;
            
            if (implementationType != null)
            {
                ValidateServiceLifetime(serviceType, implementationType, service.Lifetime);
            }
        }
    }
    
    private static void ValidateServiceLifetime(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        var scopedAttr = implementationType.GetCustomAttribute<ScopedServiceAttribute>();
        var singletonAttr = implementationType.GetCustomAttribute<SingletonServiceAttribute>();
        
        if (scopedAttr != null && lifetime != ServiceLifetime.Scoped)
        {
            throw new InvalidOperationException(
                $"Service {implementationType.Name} is marked as [ScopedService] but registered as {lifetime}");
        }
        
        if (singletonAttr != null && lifetime != ServiceLifetime.Singleton)
        {
            throw new InvalidOperationException(
                $"Service {implementationType.Name} is marked as [SingletonService] but registered as {lifetime}");
        }
    }
}
```

### Service Dependency Analysis

```csharp
public static class ServiceDependencyAnalyzer
{
    public static void AnalyzeDependencies(IServiceProvider serviceProvider)
    {
        var services = serviceProvider.GetServices<ServiceDescriptor>();
        
        foreach (var service in services)
        {
            if (service.ImplementationType != null)
            {
                AnalyzeServiceDependencies(service.ImplementationType, service.Lifetime);
            }
        }
    }
    
    private static void AnalyzeServiceDependencies(Type implementationType, ServiceLifetime lifetime)
    {
        var constructors = implementationType.GetConstructors();
        
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            
            foreach (var parameter in parameters)
            {
                ValidateDependencyLifetime(implementationType, parameter.ParameterType, lifetime);
            }
        }
    }
}
```

## Performance Implications

### Scoped Service Performance

- **Creation Overhead**: New instances created per scope
- **Memory Usage**: Higher memory usage due to multiple instances
- **Disposal**: Automatic disposal at end of scope

### Singleton Service Performance

- **Creation Overhead**: One-time creation cost
- **Memory Usage**: Lower memory usage, single instance
- **Concurrency**: Must handle concurrent access

## Best Practices Summary

### Design Principles

1. **Explicit Lifetime**: Use attributes to make service lifetime explicit
2. **Consistent Patterns**: Follow consistent registration patterns
3. **Validation**: Validate lifetime configurations during development
4. **Documentation**: Use attribute descriptions to document service purpose

### Implementation Guidelines

1. **Thread Safety**: Ensure singleton services are thread-safe
2. **Resource Management**: Properly dispose scoped services
3. **Dependency Injection**: Follow proper DI patterns and avoid service location
4. **Testing**: Consider service lifetime when writing unit tests

## Next Steps

- [Middleware Architecture](middleware-architecture.md) - Understand how services fit into the middleware pipeline

- [Testing](../getting-started/basic-usage.md) - Test services with proper lifetime scoping
