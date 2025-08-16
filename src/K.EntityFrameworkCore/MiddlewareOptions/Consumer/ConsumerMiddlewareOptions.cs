using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.MiddlewareOptions.Consumer;

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

/// <summary>
/// Consumer-specific configuration options for the AwaitForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerAwaitForgetMiddlewareOptions<T> : AwaitForgetMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the FireForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerFireForgetMiddlewareOptions<T> : FireForgetMiddlewareOptions<T>
    where T : class
{
}
