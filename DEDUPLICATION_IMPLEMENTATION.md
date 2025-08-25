# Inbox Middleware Deduplication Implementation

This document describes the implementation of message deduplication for the Inbox middleware using expression trees and xxHash64 algorithm.

## Overview

The deduplication feature prevents duplicate message processing by computing fast hashes of message content and storing them for a configurable time window. It follows the same expression tree pattern used by Producer Key configuration.

## Architecture

### 1. Expression Tree Compilation
- Uses `Expression<Func<T, object>>` for type-safe value extraction
- Compiles expressions at startup for maximum runtime performance
- Leverages the existing `ParameterReplacer` utility for expression transformation

### 2. xxHash64 Algorithm
- Extremely fast non-cryptographic hash function
- Chosen for its speed (multiple GB/s throughput)
- Produces 64-bit hashes with excellent distribution properties

### 3. GUID Storage
- 64-bit xxHash64 results are converted to GUIDs for database storage
- Uses first 8 bytes of GUID, remaining bytes are zeros
- Enables efficient primary key lookups in InboxMessage table

## Implementation Details

### InboxMiddlewareSettings<T>
```csharp
public class InboxMiddlewareSettings<T> : MiddlewareSettings<T>
{
    private Func<T, object>? deduplicationValueAccessor;
    
    public Expression<Func<T, object>>? DeduplicationValueAccessor { set; }
    
    internal Guid Hash(Envelope<T> envelope)
    {
        // Extract values using compiled accessor
        // Serialize to JSON for consistent representation
        // Compute xxHash64 and convert to GUID
    }
}
```

### InboxBuilder<T>
```csharp
public InboxBuilder<T> DeduplicateBy(Expression<Func<T, object>> valueAccessor)
{
    settings.DeduplicationValueAccessor = valueAccessor;
    return this;
}
```

## Performance Characteristics

### Hash Computation
- **xxHash64**: ~15-20 GB/s on modern CPUs
- **JSON Serialization**: Typically ~100-500 MB/s depending on object complexity
- **Overall**: Bottleneck is JSON serialization, not hashing

### Expression Compilation
- Happens once at application startup
- Runtime performance is equivalent to hand-written property access
- No reflection overhead during message processing

### Database Operations
- Primary key lookup on GUID: O(1) performance
- Indexed by hash value for efficient duplicate detection
- Cleanup operations run periodically based on `DeduplicationTimeWindow`

## Usage Patterns

### Single Property
```csharp
.DeduplicateBy(order => order.OrderId)
```

### Composite Key
```csharp
.DeduplicateBy(user => new { user.UserId, user.Email })
```

### Calculated Values
```csharp
.DeduplicateBy(payment => new { 
    payment.OrderId, 
    payment.Amount,
    DateOnly = payment.ProcessedAt.Date 
})
```

## Configuration Options

### Deduplication Time Window
```csharp
.UseDeduplicationTimeWindow(TimeSpan.FromHours(24))
```
- Controls how long duplicate detection records are kept
- Balances storage usage vs. duplicate protection
- Automatic cleanup removes expired records

## Dependencies

### NuGet Packages
- `System.IO.Hashing` (for xxHash64 algorithm)
- Added to both net8.0 and net9.0 target frameworks

### Internal Dependencies
- `ParameterReplacer` from `K.EntityFrameworkCore.Middlewares.Core`
- `InboxMessage` entity for hash storage
- `Envelope<T>` for message container

## Design Decisions

### Why xxHash64?
1. **Speed**: Fastest non-cryptographic hash available in .NET
2. **Quality**: Excellent distribution properties, low collision rate
3. **Native Support**: Available in `System.IO.Hashing` since .NET 6

### Why Expression Trees?
1. **Type Safety**: Compile-time checking of property access
2. **Performance**: No reflection overhead at runtime
3. **Consistency**: Matches existing Producer Key implementation pattern

### Why GUID Storage?
1. **Database Compatibility**: Universal support across all databases
2. **Primary Key Efficiency**: Fast lookups and indexing
3. **Simple Conversion**: Easy to convert 64-bit hash to 128-bit GUID

## Future Enhancements

### Potential Improvements
1. **Custom Hash Functions**: Support for other hash algorithms
2. **Batch Cleanup**: More efficient cleanup of expired records
3. **Metrics**: Expose deduplication hit rates and performance metrics
4. **Custom Serialization**: Alternatives to JSON for specific scenarios

### Backward Compatibility
- All changes are additive and non-breaking
- Existing code without deduplication continues to work unchanged
- Default behavior (no deduplication) remains the same
