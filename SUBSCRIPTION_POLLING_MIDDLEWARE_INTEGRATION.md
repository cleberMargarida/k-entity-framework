# Subscription and Polling Middleware Integration

## Overview

This document describes the refactoring that integrates `SubscriptionRegistry` and `PollerManager` functionality into specialized middlewares, improving the extensibility of the consumption pipeline.

## Problem

Previously, the `ConsumerEnumerator` in `Topic<T>` had a direct dependency on `ISubscriptionRegistry<T>`:

```csharp
activationToken = serviceProvider.GetRequiredService<ISubscriptionRegistry<T>>().Activate();
```

This approach broke the extensibility provided by the middleware approach because:

1. **Tight Coupling**: The consumer was directly responsible for subscription management
2. **Limited Extensibility**: Custom subscription logic couldn't be easily injected
3. **Separation of Concerns**: Subscription management was mixed with enumeration logic
4. **Inconsistent Architecture**: Other functionalities use middleware, but subscription didn't

## Solution

### New Middleware Architecture

Two new specialized middlewares have been created:

#### 1. SubscriptionMiddleware<T>

**Purpose**: Manages subscription lifecycle within the middleware pipeline.

**Key Features**:
- Activates subscriptions lazily when first invoked
- Properly disposes subscriptions when the middleware is disposed
- Integrates seamlessly with the existing middleware chain
- Maintains subscription state throughout the consumer lifecycle

**Location**: `src/K.EntityFrameworkCore/Middlewares/Core/SubscriptionMiddleware.cs`

#### 2. PollingMiddleware<T>

**Purpose**: Manages polling lifecycle and ensures pollers are started appropriately.

**Key Features**:
- Starts shared or dedicated pollers based on configuration
- Handles both exclusive and shared connection modes
- Ensures pollers are started before message consumption begins
- Works in conjunction with the existing `IPollerManager`

**Location**: `src/K.EntityFrameworkCore/Middlewares/Core/PollingMiddleware.cs`

### Updated Consumer Pipeline

The `ConsumerMiddlewareInvoker<T>` now includes the new middlewares in the correct order:

```csharp
public ConsumerMiddlewareInvoker(
      SubscriptionMiddleware<T> subscriptionMiddleware      // NEW: Handles subscription lifecycle
    , PollingMiddleware<T> pollingMiddleware               // NEW: Handles polling lifecycle  
    , ConsumerMiddleware<T> consumerMiddleware             // Existing: Handles message consumption
    , DeserializerMiddleware<T> deserializationMiddleware  // Existing: Handles deserialization
    , InboxMiddleware<T> inboxMiddleware                   // Existing: Handles inbox processing
    )
{
    Use(subscriptionMiddleware);
    Use(pollingMiddleware);
    Use(consumerMiddleware);
    Use(deserializationMiddleware);
    Use(inboxMiddleware);
}
```

### Simplified Topic<T> Implementation

The `ConsumerEnumerator` is now much cleaner:

```csharp
internal ConsumerEnumerator(IServiceProvider serviceProvider, CancellationToken cancellationToken)
{
    middleware = serviceProvider.GetRequiredService<ConsumerMiddlewareInvoker<T>>();
    this.cancellationToken = cancellationToken;
}
```

The subscription management is now handled entirely by the middleware pipeline.

## Benefits

### 1. **Improved Extensibility**
- Custom subscription logic can be implemented by replacing or extending the `SubscriptionMiddleware<T>`
- Polling behavior can be customized through the `PollingMiddleware<T>`
- Additional middleware can be inserted before or after subscription/polling setup

### 2. **Better Separation of Concerns**
- `Topic<T>` focuses only on enumeration
- Subscription management is isolated in its own middleware
- Polling management is isolated in its own middleware
- Each component has a single responsibility

### 3. **Consistent Architecture**
- All functionality now follows the middleware pattern
- Uniform approach to dependency injection and lifecycle management
- Consistent error handling and cancellation support

### 4. **Enhanced Testability**
- Each middleware can be unit tested independently
- Subscription and polling logic can be mocked easily
- Consumer enumeration logic is simplified and easier to test

### 5. **Flexible Configuration**
- Middleware settings can be configured independently
- Easy to enable/disable specific functionality
- Support for custom middleware injection through DI

## Backward Compatibility

The changes are fully backward compatible:

- **Public API**: No changes to public interfaces
- **Configuration**: Existing configuration continues to work
- **Behavior**: The same subscription and polling behavior is maintained
- **Performance**: No performance degradation; possibly improved due to better separation

## Usage Examples

### Custom Subscription Middleware

```csharp
public class CustomSubscriptionMiddleware<T> : SubscriptionMiddleware<T>
{
    public CustomSubscriptionMiddleware(IServiceProvider serviceProvider, SubscriptionMiddlewareSettings<T> settings) 
        : base(serviceProvider, settings)
    {
    }

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // Custom subscription logic before activation
        LogSubscriptionEvent($"Activating subscription for {typeof(T).Name}");
        
        await base.InvokeAsync(envelope, cancellationToken);
        
        // Custom logic after processing
        LogSubscriptionEvent($"Processed message for {typeof(T).Name}");
    }
}
```

### Custom Polling Middleware

```csharp
public class CustomPollingMiddleware<T> : PollingMiddleware<T>
{
    public CustomPollingMiddleware(IServiceProvider serviceProvider, PollingMiddlewareSettings<T> settings) 
        : base(serviceProvider, settings)
    {
    }

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // Custom polling logic
        await EnsureHealthyConnection();
        
        await base.InvokeAsync(envelope, cancellationToken);
    }
}
```

## Migration Guide

For users extending the framework:

### Before (Not Recommended)
```csharp
// Direct dependency on subscription registry
var registry = serviceProvider.GetRequiredService<ISubscriptionRegistry<T>>();
var token = registry.Activate();
```

### After (Recommended)
```csharp
// Use middleware pipeline for extensibility
public class MyCustomMiddleware<T> : Middleware<T>
{
    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // Your custom logic here
        await base.InvokeAsync(envelope, cancellationToken);
    }
}
```

## Technical Details

### Dependency Injection Changes

New registrations added to `KafkaOptionsExtension`:

```csharp
// Subscription and polling middlewares for better extensibility
services.AddSingleton(typeof(SubscriptionMiddlewareSettings<>));
services.AddScoped(typeof(SubscriptionMiddleware<>));
services.AddSingleton(typeof(PollingMiddlewareSettings<>));
services.AddScoped(typeof(PollingMiddleware<>));
```

### Lifecycle Management

- **SubscriptionMiddleware**: Implements `IDisposable` to properly clean up subscription tokens
- **PollingMiddleware**: Leverages existing `IPollerManager` for lifecycle management
- **Topic<T>**: Simplified lifecycle with middleware handling complexity

## Future Enhancements

This architecture enables several future enhancements:

1. **Conditional Subscriptions**: Middleware that subscribes based on runtime conditions
2. **Subscription Pooling**: Middleware that pools subscriptions across multiple consumers
3. **Health Monitoring**: Middleware that monitors subscription and polling health
4. **Metrics Collection**: Middleware that collects subscription and polling metrics
5. **Circuit Breaker**: Middleware that implements circuit breaker patterns for subscriptions

## Conclusion

The integration of subscription and polling functionality into specialized middlewares significantly improves the extensibility and maintainability of the k-entity-framework consumer pipeline while maintaining full backward compatibility. This change aligns with the framework's middleware-first architecture and provides a solid foundation for future enhancements.
