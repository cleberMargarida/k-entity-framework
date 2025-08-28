# Service Attribute Decoration Summary

This document summarizes all the classes that have been decorated with service lifetime attributes (`[ScopedService]` or `[SingletonService]`) based on their registration in the `KafkaOptionsExtension` class.

## Purpose

These attributes help facilitate debugging by clearly marking which classes are registered as singleton or scoped services in the dependency injection container. This makes it easier to understand the service lifetime and identify potential issues during development.

## Decorated Classes

### Scoped Services (`[ScopedService]`)

These classes are registered with `services.AddScoped()` and have a new instance created for each scope:

1. **ScopedCommandRegistry** - `Extensions/DbContextExtensions.cs`
2. **ConsumerMiddlewareInvoker<T>** - `Middlewares/Consumer/ConsumerMiddlewareInvoker.cs`
3. **ProducerMiddlewareInvoker<T>** - `Middlewares/Producer/ProducerMiddlewareInvoker.cs`
4. **InboxMiddleware<T>** - `Middlewares/Inbox/InboxMiddleware.cs`
5. **SubscriptionMiddleware<T>** - `Middlewares/Consumer/SubscriptionMiddleware.cs` *(already decorated)*
6. **PollingMiddleware<T>** - `Middlewares/Consumer/PollingMiddleware.cs` *(already decorated)*
7. **DeserializerMiddleware<T>** - `Middlewares/Serialization/DeserializerMiddleware.cs` *(already decorated)*
8. **ProducerMiddleware<T>** - `Middlewares/Producer/ProducerMiddleware.cs`
9. **SerializerMiddleware<T>** - `Middlewares/Serialization/SerializerMiddleware.cs` *(already decorated)*
10. **ProducerForgetMiddleware<T>** - `Middlewares/Forget/ProducerForgetMiddleware.cs`
11. **OutboxMiddleware<T>** - `Middlewares/Outbox/OutboxMiddleware.cs`

### Singleton Services (`[SingletonService]`)

These classes are registered with `services.AddSingleton()` and have a single instance shared across the application:

1. **SerializationMiddlewareSettings<T>** - `Middlewares/Serialization/SerializationMiddlewareSettings.cs`
2. **ClientSettings<T>** - `Middlewares/Core/ClientSettings.cs`
3. **InboxMiddlewareSettings<T>** - `Middlewares/Inbox/InboxMiddlewareSettings.cs`
4. **SubscriptionMiddlewareSettings<T>** - `Middlewares/Consumer/SubscriptionMiddleware.cs`
5. **PollingMiddlewareSettings<T>** - `Middlewares/Consumer/PollingMiddleware.cs`
6. **SubscriptionRegistry<T>** - `Middlewares/Consumer/SubscriptionRegistry.cs`
7. **ConsumerMiddlewareSettings<T>** - `Middlewares/Consumer/ConsumerMiddlewareSettings.cs`
8. **ConsumerMiddleware<T>** - `Middlewares/Consumer/ConsumerMiddleware.cs`
9. **ProducerMiddlewareSettings<T>** - `Middlewares/Producer/ProducerMiddlewareSettings.cs`
10. **ProducerForgetMiddlewareSettings<T>** - `Middlewares/Forget/ProducerForgetMiddlewareSettings.cs`
11. **OutboxMiddlewareSettings<T>** - `Middlewares/Outbox/OutboxMiddlewareSettings.cs`
12. **OutboxProducerMiddleware<T>** - `Middlewares/Outbox/OutboxProducerMiddleware.cs`
13. **KafkaConsumerPollService** - `KafkaConsumerPollService.cs`
14. **PollerManager** - `Middlewares/Consumer/PollerManager.cs`

## Additional Notes

- Some classes were already decorated with the appropriate attributes before this update
- The namespace imports were fixed to resolve compilation errors with invalid namespace references
- All changes maintain the existing functionality while adding clear service lifetime documentation
- The build is successful with only warnings (no errors)

## Benefits

1. **Enhanced Debugging**: Developers can quickly identify service lifetimes when debugging DI-related issues
2. **Documentation**: Serves as inline documentation for service registration patterns
3. **Consistency**: Ensures all services registered in `KafkaOptionsExtension` are properly marked
4. **Maintenance**: Makes it easier to understand and maintain the service registration code

## Future Considerations

Consider using these attributes in combination with source generators or analyzers to automatically validate that the attributes match the actual DI registrations.
