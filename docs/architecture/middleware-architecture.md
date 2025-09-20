# Middleware Architecture

K-Entity-Framework uses a pipeline pattern where messages flow through configurable middleware components.

## Envelope Structure

The `Envelope<T>` is the lightweight container that flows through the middleware pipeline. 

See the API reference for the `Envelope<T>` type: [K.EntityFrameworkCore.Envelope<T>](/api/K.EntityFrameworkCore.Envelope-1.html)

## Middleware Interface

See the API reference for the `IMiddleware<T>` interface: [K.EntityFrameworkCore.Interfaces.IMiddleware<T>](/api/K.EntityFrameworkCore.Interfaces.IMiddleware-1.html)

Middleware can:
- Transform messages
- Chain to next middleware

## Pipeline Flow

```mermaid
graph LR
    A[Message Input] --> B[MiddlewareInvoker<T>]
    B --> C[Linked List Pipeline]
    C --> D[Middleware 1]
    D --> E[Middleware 2] 
    E --> F[Middleware N]
    F --> G[Business Logic]

    classDef default fill:#2b2b2b,stroke:#666,color:#eee;

    class A input;
    classDef input fill:#0d47a1,stroke:#29b6f6,color:#bbdefb;

    class B invoker;
    classDef invoker fill:#263238,stroke:#90a4ae,color:#eceff1;

    class C pipeline;
    classDef pipeline fill:#3e2723,stroke:#ff9800,color:#ffcc80;

    class G logic;
    classDef logic fill:#1b5e20,stroke:#66bb6a,color:#a5d6a7;
```
## Configuration

Middleware is automatically enabled when you call `HasXXX()` methods:

```csharp
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasProducer(producer =>
    {
        producer.HasKey(o => o.OrderId.ToString());
        producer.HasOutbox();  // Enables OutboxMiddleware
    });
    
    topic.HasConsumer(consumer =>
    {
        consumer.HasInbox();   // Enables InboxMiddleware
        consumer.HasMaxBufferedMessages(100);
    });
});
```

### Specific Pipeline Implementations

**Producer Pipeline**:
```csharp
internal class ProducerMiddlewareInvoker<T> : MiddlewareInvoker<T>
{
    public ProducerMiddlewareInvoker(
          SerializerMiddleware<T> serializationMiddleware
        , OutboxMiddleware<T> outboxMiddleware
        , ProducerMiddleware<T> producerMiddleware)
    {
        Use(serializationMiddleware);
        Use(outboxMiddleware);
        Use(producerMiddleware);
    }
}
```

**Consumer Pipeline**:
```csharp
internal class ConsumerMiddlewareInvoker<T> : MiddlewareInvoker<T>
{
    public ConsumerMiddlewareInvoker(
          SubscriberMiddleware<T> subscriberMiddleware
        , ConsumerMiddleware<T> consumerMiddleware
        , DeserializerMiddleware<T> deserializationMiddleware
        , HeaderFilterMiddleware<T> headerFilterMiddleware
        , InboxMiddleware<T> inboxMiddleware)
    {
        Use(subscriberMiddleware);
        Use(consumerMiddleware);
        Use(deserializationMiddleware);
        Use(headerFilterMiddleware);
        Use(inboxMiddleware);
    }
}
```

### Built-in Middleware Types

#### Producer Middleware
- **SerializerMiddleware**: Handles message serialization to bytes
- **OutboxMiddleware**: Implements transactional outbox pattern  
- **ProducerForgetMiddleware**: Provides fire-and-forget or await-forget modes
- **ProducerMiddleware**: Produces messages to Kafka

#### Consumer Middleware
- **SubscriberMiddleware**: Manages Kafka subscription lifecycle
- **ConsumerMiddleware**: Handles message consumption from topics
- **DeserializerMiddleware**: Deserializes messages from bytes
- **HeaderFilterMiddleware**: Filters messages based on headers
- **InboxMiddleware**: Implements message deduplication pattern

### Service Lifetimes

Middleware components are registered with the dependency injection container using appropriate service lifetimes:

- **Singleton**: Core Kafka clients (producers/consumers), client configurations, and shared infrastructure like `ConsumerPollRegistry` and `Channel<T>`
- **Scoped**: Middleware invokers, individual middleware components, and their associated settings
