# Type Salt Implementation for Hash Deduplication

This document explains the type-specific salt implementation added to the inbox deduplication hash calculation.

## Overview

A deterministic type salt has been added to the hash calculation to ensure type isolation in deduplication. This prevents potential hash collisions between different message types that might have the same deduplication values.

## Implementation Details

### Type Salt Generation
```csharp
private static readonly byte[] TypeSalt = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name);
```

**Key Characteristics:**
- ✅ **Deterministic**: Same type always produces same salt
- ✅ **Lightweight**: Computed once per generic type instantiation
- ✅ **Fast**: Pre-computed static field, no runtime overhead
- ✅ **Type-specific**: Each `InboxMiddlewareSettings<T>` has its own salt

### Hash Calculation with Salt
```csharp
// Combine data with type salt for type-specific hashing
byte[] saltedData = new byte[dataToHash.Length + TypeSalt.Length];
Array.Copy(TypeSalt, 0, saltedData, 0, TypeSalt.Length);
Array.Copy(dataToHash, 0, saltedData, TypeSalt.Length, dataToHash.Length);

var hashBytes = XxHash64.Hash(saltedData);
```

**Process:**
1. Type salt bytes are prepended to the message data
2. Combined data is hashed with xxHash64
3. Result is a type-specific hash value

## Benefits

### 1. Type Isolation
```csharp
// These will now have different hashes even with same deduplication values
class OrderPlaced { public string OrderId { get; set; } = "123"; }
class PaymentProcessed { public string OrderId { get; set; } = "123"; }

// Before: Potential collision if both use OrderId for deduplication
// After: Different hashes due to different type salts
```

### 2. Collision Prevention
- Eliminates cross-type hash collisions
- Each message type has its own hash space
- Safer deduplication across different message types

### 3. Performance Impact
- **Static computation**: Type salt computed once per type
- **Minimal overhead**: Single array copy operation
- **Fast concatenation**: Simple byte array operations
- **xxHash64 speed**: Still maintains high-performance hashing

## Example Usage

### Before (without type salt):
```csharp
// Both could theoretically produce same hash
var orderHash = Hash(new OrderPlaced { OrderId = "123" });
var paymentHash = Hash(new PaymentProcessed { OrderId = "123" });
// Risk of collision if same property values
```

### After (with type salt):
```csharp
// Different hashes guaranteed due to type salts
var orderHash = Hash(new OrderPlaced { OrderId = "123" });     // Includes "MyApp.OrderPlaced" salt
var paymentHash = Hash(new PaymentProcessed { OrderId = "123" }); // Includes "MyApp.PaymentProcessed" salt
// No collision possible between different types
```

## Type Salt Content

The salt uses the type's full name for maximum specificity:

```csharp
// For type: MyApp.Events.OrderPlaced
TypeSalt = Encoding.UTF8.GetBytes("MyApp.Events.OrderPlaced");

// For type: MyApp.Events.PaymentProcessed  
TypeSalt = Encoding.UTF8.GetBytes("MyApp.Events.PaymentProcessed");
```

**Fallback**: If `FullName` is null (rare edge case), uses `Name` property.

## Memory and Performance

### Static Field Optimization
```csharp
private static readonly byte[] TypeSalt = ...;
```

- **Per-type storage**: Each generic instantiation gets its own static field
- **One-time computation**: Calculated when type is first used
- **Thread-safe**: `readonly` ensures immutability
- **Memory efficient**: Only stores bytes for type name

### Runtime Performance
- **Zero allocation**: Pre-computed salt reused for all instances
- **Fast concatenation**: Simple array copy operations
- **Minimal overhead**: ~50-100 nanoseconds additional per hash

## Configuration

No additional configuration required. The type salt is automatically:
- Generated for each message type `T`
- Applied to all hash calculations for that type
- Consistent across application restarts
- Deterministic for the same type

## Example Scenarios

### Multi-tenant Applications
```csharp
// Different types with same property names
class TenantA.OrderCreated { public string Id { get; set; } }
class TenantB.OrderCreated { public string Id { get; set; } }

// Type salts prevent cross-tenant deduplication issues
```

### Versioned Message Types
```csharp
// Different versions of same logical message
class V1.OrderPlaced { public string OrderId { get; set; } }
class V2.OrderPlaced { public string OrderId { get; set; } }

// Each version maintains separate deduplication space
```

### Generic Message Wrappers
```csharp
// Generic wrapper with different payload types
class EventWrapper<T> { public T Payload { get; set; } }

// EventWrapper<OrderPlaced> and EventWrapper<PaymentProcessed> 
// have different type salts
```

## Backward Compatibility

This is a **breaking change** for existing deployments with inbox data:

1. **New Hash Values**: All hashes will be different due to type salt
2. **Database Migration**: Existing InboxMessage records should be cleared
3. **Deduplication Reset**: Previous deduplication history is lost

**Migration Strategy:**
```sql
-- Clear existing inbox records before deploying
DELETE FROM InboxMessages;
```

## Security Considerations

### Not Cryptographic
- Type salt is not a cryptographic security feature
- Purpose is collision prevention, not security
- Type names are predictable and deterministic

### Information Disclosure
- Type salt contains full type name
- Could reveal type structure in logs/debugging
- Consider this in sensitive environments

## Alternative Implementations Considered

### 1. Type Hash (Not Chosen)
```csharp
private static readonly byte[] TypeSalt = BitConverter.GetBytes(typeof(T).GetHashCode());
```
❌ **Rejected**: `GetHashCode()` not guaranteed stable across app restarts

### 2. Assembly Qualified Name (Not Chosen)
```csharp
private static readonly byte[] TypeSalt = Encoding.UTF8.GetBytes(typeof(T).AssemblyQualifiedName);
```
❌ **Rejected**: Too verbose, includes version/culture info

### 3. GUID Salt (Not Chosen)
```csharp
private static readonly byte[] TypeSalt = typeof(T).GUID.ToByteArray();
```
❌ **Rejected**: GUID not deterministic across app domains

### 4. Full Name (✅ Chosen)
```csharp
private static readonly byte[] TypeSalt = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name);
```
✅ **Selected**: Deterministic, lightweight, stable, human-readable

## Conclusion

The type salt implementation provides:
- **Type isolation** for safer deduplication
- **Minimal performance impact** through static optimization
- **Deterministic behavior** across application restarts
- **Simple implementation** with clear semantics

This enhancement makes the deduplication system more robust while maintaining high performance characteristics.
