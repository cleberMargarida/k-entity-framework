# Middleware Lifetime Issues Analysis

## Overview

This document analyzes critical lifetime management issues found in the k-entity-framework middleware pipeline that can cause problems when running in different service scopes. These issues can lead to memory leaks, concurrency problems, resource management failures, and unexpected behavior in multi-tenant or high-concurrency scenarios.

## 🚨 Critical Issues Identified

### 1. Singleton/Scoped Service Lifetime Mismatches

**Location**: `src/K.EntityFrameworkCore/Extensions/KafkaOptionsExtension.cs`

**Problem**: The framework mixes singleton and scoped services creating captive dependencies.

```csharp
// PROBLEMATIC REGISTRATIONS:
services.AddSingleton(typeof(ConsumerMiddleware<>));  // Singleton middleware ❌
services.AddScoped(typeof(InboxMiddleware<>));        // Scoped middleware
services.AddScoped(typeof(ConsumerMiddlewareInvoker<>)); // Scoped invoker

// Consumer middleware invoker construction:
public ConsumerMiddlewareInvoker<T>(
    SubscriptionMiddleware<T> subscriptionMiddleware,    // Scoped
    PollingMiddleware<T> pollingMiddleware,             // Scoped  
    ConsumerMiddleware<T> consumerMiddleware,           // Singleton ❌
    DeserializerMiddleware<T> deserializationMiddleware, // Scoped
    InboxMiddleware<T> inboxMiddleware                   // Scoped
)
```

**Impact**:
- `ConsumerMiddleware<T>` is registered as **Singleton** but injected into a **Scoped** invoker
- Creates captive dependency where singleton middleware captures the first scoped service provider
- Subsequent requests use stale service references
- Can cause memory leaks and incorrect behavior across requests

**Evidence**:
```csharp
// From KafkaOptionsExtension.cs line 53
services.AddSingleton(typeof(ConsumerMiddleware<>));
```

### 2. Static State in SubscriptionRegistry

**Location**: `src/K.EntityFrameworkCore/Middlewares/Consumer/SubscriptionRegistry.cs`

**Problem**: Uses static fields for managing subscription reference counting across all message types.

```csharp
[SingletonService]
internal sealed class SubscriptionRegistry<T>
{
    private static readonly Lock gate = new();
    private static int refCount;  // ❌ STATIC SHARED ACROSS ALL MESSAGE TYPES
    
    public IDisposable Activate()
    {
        lock (gate)
        {
            if (refCount == 0) { /* subscribe */ }
            refCount++;  // Shared counter for all T types!
        }
    }
}
```

**Impact**:
- **Cross-type pollution**: `refCount` is static but should be per-type T
- **Concurrency problems**: Different service scopes processing different message types interfere with each other
- **Memory leaks**: Static references prevent proper garbage collection
- **Race conditions**: Multiple types can incorrectly affect each other's subscription state

**Evidence**:
```csharp
// From SubscriptionRegistry.cs lines 18-20
private static readonly Lock gate = new();
private static int refCount;
```

### 3. DbContext Scope Boundary Violations

**Location**: `src/K.EntityFrameworkCore/Middlewares/Inbox/InboxMiddleware.cs`

**Problem**: Scoped middlewares depend on `ICurrentDbContext` but pipeline execution might span multiple scopes.

```csharp
[ScopedService]
internal class InboxMiddleware<T>(
    ICurrentDbContext currentDbContext,  // Scoped to specific DbContext
    ScopedCommandRegistry scopedCommandRegistry,
    InboxMiddlewareSettings<T> settings) 
{
    private readonly DbContext context = currentDbContext.Context;  // Captured at construction
    
    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // Uses captured context - may be stale or disposed
        DbSet<InboxMessage> inboxMessages = context.Set<InboxMessage>();
    }
}
```

**Impact**:
- **Stale DbContext**: If middleware is reused across requests, it holds onto a disposed DbContext
- **Transaction boundary confusion**: Commands may execute against wrong DbContext
- **Resource leaks**: DbContext not properly disposed in some execution paths
- **Data integrity issues**: Operations on wrong database context

**Evidence**:
```csharp
// From InboxMiddleware.cs line 22
private readonly DbContext context = currentDbContext.Context;
```

### 4. PollerManager Service Resolution Anti-patterns

**Location**: `src/K.EntityFrameworkCore/Middlewares/Consumer/PollerManager.cs`

**Problem**: Creates consumers using root service provider rather than scoped providers.

```csharp
[SingletonService]
internal sealed class PollerManager(IServiceProvider serviceProvider) : IPollerManager
{
    public void EnsureDedicatedStarted(Type type)
    {
        var poller = dedicated.GetOrAdd(type, static (t, sp) =>
            new KafkaConsumerPollService(sp, () => sp.GetRequiredKeyedService<IConsumer>(t)), 
            serviceProvider);  // ❌ Uses root provider, not scoped
    }
}
```

**Impact**:
- **Wrong service provider**: Uses root service provider instead of current request scope
- **Resource management**: Consumers created outside proper disposal scope
- **Configuration isolation**: Different requests can't have different consumer configurations
- **Dependency injection violations**: Services resolved from wrong container scope

**Evidence**:
```csharp
// From PollerManager.cs lines 25-27
var poller = dedicated.GetOrAdd(type, static (t, sp) =>
    new KafkaConsumerPollService(sp, () => sp.GetRequiredKeyedService<IConsumer>(t)), 
    serviceProvider);
```

### 5. ScopedCommandRegistry Execution Timing Issues

**Location**: `src/K.EntityFrameworkCore/Extensions/ScopedCommandRegistry.cs`

**Problem**: Defers command execution but may execute in wrong scope.

```csharp
public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
{
    while (commands.TryDequeue(out var command))
        await command.Invoke(serviceProvider, cancellationToken);  // May be wrong provider
}
```

**Impact**:
- **Scope mismatch**: Commands enqueued in one scope but executed in another
- **DbContext confusion**: Commands may run against wrong or disposed DbContext
- **Transaction boundaries**: Commands outside intended transaction scope
- **Data consistency issues**: Operations not properly grouped in transactions

**Evidence**:
```csharp
// From ScopedCommandRegistry.cs lines 17-19
public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
{
    while (commands.TryDequeue(out var command))
        await command.Invoke(serviceProvider, cancellationToken);
}
```

### 6. Consumer Service Provider Resolution Issues

**Location**: Multiple files using consumer resolution pattern

**Problem**: Inconsistent service provider usage for consumer resolution.

```csharp
// From InboxMiddleware.cs CommitMiddlewareInvokeCommand
public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
{
    // We need to resolve settings again to choose the correct consumer
    var settings = serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>();

    IConsumer consumer;

    if (settings.ExclusiveConnection)
    {
        consumer = serviceProvider.GetRequiredKeyedService<IConsumer>(typeof(T));
    }
    else
    {
        consumer = serviceProvider.GetRequiredService<IConsumer>();
    }
    // ... consumer operations with potentially wrong provider
}
```

**Impact**:
- **Service provider confusion**: May resolve from wrong scope
- **Consumer lifecycle issues**: Consumers not properly managed
- **Resource leaks**: Consumers not disposed correctly

## 🔧 Recommended Solutions

### 1. Fix Service Lifetime Registrations

**Change**: Update `KafkaOptionsExtension.cs` to use consistent scoped registration for all middlewares.

```csharp
// CORRECTED REGISTRATIONS:
public void ApplyServices(IServiceCollection services)
{
    // ALL middlewares should be scoped to match the invoker
    services.AddScoped(typeof(ConsumerMiddleware<>));        // ✅ Change to Scoped
    services.AddScoped(typeof(InboxMiddleware<>));           // ✅ Already correct
    services.AddScoped(typeof(SubscriptionMiddleware<>));    // ✅ Already correct
    services.AddScoped(typeof(PollingMiddleware<>));         // ✅ Already correct
    services.AddScoped(typeof(DeserializerMiddleware<>));    // ✅ Already correct
    
    // Settings remain singleton (stateless configuration)
    services.AddSingleton(typeof(ConsumerMiddlewareSettings<>)); // ✅ Correct
    
    // Fix the registry to be per-type, not static
    services.AddSingleton(typeof(ISubscriptionRegistry<>), typeof(SubscriptionRegistry<>));
}
```

### 2. Fix SubscriptionRegistry Static State

**Change**: Remove static fields and make state instance-based per type T.

```csharp
[SingletonService]
internal sealed class SubscriptionRegistry<T> : ISubscriptionRegistry<T>
    where T : class
{
    // ✅ Remove static - make instance-based per type T
    private readonly object gate = new();
    private int refCount;  // ✅ Instance field, not static
    
    // ✅ Store activations per instance
    private readonly ConcurrentDictionary<object, DeactivationToken> activeSubscriptions = new();
    
    public IDisposable Activate()
    {
        lock (gate)
        {
            if (refCount == 0) { /* subscribe */ }
            refCount++;  // ✅ Now per-type instance
        }
        return new DeactivationToken(this, serviceProvider, clientSettings.TopicName);
    }
}
```

### 3. Fix DbContext Scope Management

**Change**: Use factory pattern instead of direct DbContext injection.

```csharp
[ScopedService]
internal class InboxMiddleware<T>(
    Func<DbContext> dbContextFactory,  // ✅ Use factory instead of direct injection
    ScopedCommandRegistry scopedCommandRegistry,
    InboxMiddlewareSettings<T> settings) 
    : Middleware<T>(settings)
{
    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        using var context = dbContextFactory();  // ✅ Get fresh context for each operation
        
        ulong hashId = settings.Hash(envelope);
        DbSet<InboxMessage> inboxMessages = context.Set<InboxMessage>();
        
        // ... rest of implementation with fresh context
    }
}
```

### 4. Fix PollerManager Scope Issues

**Change**: Accept and use scoped service provider for middleware resolution.

```csharp
[SingletonService]  
internal sealed class PollerManager : IPollerManager
{
    public void EnsureDedicatedStarted(Type type, IServiceProvider scopedProvider)  // ✅ Accept scoped provider
    {
        var poller = dedicated.GetOrAdd(type, static (t, providers) =>
        {
            var (root, scoped) = providers;
            return new KafkaConsumerPollService(
                scoped,  // ✅ Use scoped provider for middleware resolution
                () => root.GetRequiredKeyedService<IConsumer>(t)  // Root for consumer (singleton)
            );
        }, (serviceProvider, scopedProvider));  // ✅ Pass both providers
    }
}
```

### 5. Fix ScopedCommandRegistry Execution Context

**Change**: Capture service scope when commands are added and use it during execution.

```csharp
[ScopedService]
internal class ScopedCommandRegistry
{
    private readonly Queue<(ScopedCommand command, IServiceProvider scope)> commands = new();
    
    public void Add(ScopedCommand command, IServiceProvider currentScope)  // ✅ Capture scope
    {
        commands.Enqueue((command, currentScope));
    }
    
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        while (commands.TryDequeue(out var item))
        {
            await item.command.Invoke(item.scope, cancellationToken);  // ✅ Use captured scope
        }
    }
}
```

## 📋 Additional Recommendations

### 1. Add Scope Validation
- Implement runtime validation to detect lifetime mismatches
- Add logging when services are resolved from wrong scopes
- Consider using `IServiceScope` explicitly for critical operations

### 2. Improve Resource Management
- Implement proper `IAsyncDisposable` patterns for async cleanup
- Add timeout handling for long-running operations
- Ensure all background tasks respect cancellation tokens

### 3. Enhance Documentation
- Document the expected service lifetime for each component
- Provide clear guidance on when to use singleton vs scoped services
- Add examples of proper dependency injection patterns

### 4. Consider Using Factory Patterns
- Replace direct service injection with factory patterns where scopes matter
- Use `IServiceScopeFactory` for creating dedicated scopes when needed
- Implement middleware factories that create instances per request

### 5. Add Integration Tests
- Create tests that verify correct behavior across multiple service scopes
- Test concurrent processing of different message types
- Validate proper resource cleanup and disposal

## Impact Assessment

### Severity: **HIGH**
These issues can cause:
- Memory leaks in production environments
- Data corruption due to wrong DbContext usage
- Concurrency bugs that are hard to reproduce
- Resource exhaustion in high-load scenarios
- Unpredictable behavior in multi-tenant applications

### Priority: **IMMEDIATE**
These are fundamental architectural issues that affect the reliability and scalability of the framework.

## Testing Strategy

After implementing fixes:

1. **Unit Tests**: Verify each middleware works correctly in isolation
2. **Integration Tests**: Test full pipeline with multiple message types
3. **Concurrency Tests**: Verify thread safety and proper scope isolation
4. **Load Tests**: Ensure no memory leaks under sustained load
5. **Multi-tenant Tests**: Verify proper isolation between different contexts

## Conclusion

The identified lifetime management issues represent critical flaws in the middleware architecture that must be addressed to ensure reliable operation in production environments. The recommended fixes provide a clear path to resolution while maintaining the framework's functionality and performance characteristics.

---

**Analysis Date**: August 28, 2025  
**Analyzed Files**: 15+ core middleware and service registration files  
**Severity Level**: Critical  
**Recommended Action**: Immediate implementation of suggested fixes
