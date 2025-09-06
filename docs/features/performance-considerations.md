# Performance Considerations

K-Entity-Framework is designed for high performance with minimal memory usage.

- **Stack allocation**: Less garbage collection overhead
- **Zero-copy payload**: No data copying during processing
- **Thread-safe headers**: Safe for concurrent access
- **Fast header extraction**: Compiled expressions cache header accessors