namespace K.EntityFrameworkCore.MiddlewareOptions.Consumer;

public class ClientMiddlewareOptions<T> : MiddlewareOptions<T>
    where T : class
{
    public string TopicName { get; set; }
}

public class ConsumerMiddlewareOptions<T>(ClientMiddlewareOptions<T> clientOptions) : ClientMiddlewareOptions<T>
    where T : class
{
}

public class ProducerMiddlewareOptions<T>(ClientMiddlewareOptions<T> clientOptions) : ClientMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerRetryMiddlewareOptions<T> : RetryMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerCircuitBreakerMiddlewareOptions<T> : CircuitBreakerMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the ThrottleMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerThrottleMiddlewareOptions<T> : ThrottleMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerBatchMiddlewareOptions<T> : BatchMiddlewareOptions<T>
    where T : class
{
}
