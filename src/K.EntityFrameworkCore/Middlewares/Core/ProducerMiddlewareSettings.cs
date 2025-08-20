using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Batch;
using K.EntityFrameworkCore.Middlewares.CircuitBreaker;
using K.EntityFrameworkCore.Middlewares.Retry;
using System.Linq.Expressions;

namespace K.EntityFrameworkCore.Middlewares.Core;

/// <summary>
/// Helper class to replace parameter expressions in expression trees.
/// </summary>
internal class ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == oldParameter ? newParameter : base.VisitParameter(node);
    }
}


internal class ProducerMiddlewareSettings<T>(ClientSettings<T> clientSettings) : MiddlewareSettings<T>(true)
    where T : class
{
    private Func<T, string>? keyAccessor;

    private readonly ProducerConfig producerConfig = new(clientSettings.ClientConfig);
    public IEnumerable<KeyValuePair<string, string>> ProducerConfig => producerConfig;
    public string TopicName => clientSettings.TopicName;

    public Expression KeyPropertyAccessor
    {
        set
        {
            if (value == null)
            {
                keyAccessor = null;
                return;
            }

            var parameter = Expression.Parameter(typeof(T), "entity");

            Expression propertyExpression;
            if (value is LambdaExpression lambda)
            {
                // Replace the lambda's parameter with our new parameter
                var parameterReplacer = new ParameterReplacer(lambda.Parameters[0], parameter);
                propertyExpression = parameterReplacer.Visit(lambda.Body);
            }
            else
            {
                propertyExpression = value;
            }

            var toStringMethod = typeof(object).GetMethod("ToString")!;
            var toStringCall = Expression.Call(propertyExpression, toStringMethod);
            var lambdaExpression = Expression.Lambda<Func<T, string>>(toStringCall, parameter);

            keyAccessor = lambdaExpression.Compile();
        }
    }

    /// <summary>
    /// Gets the key value for the specified entity instance.
    /// </summary>
    /// <param name="entity">The entity instance to extract the key from.</param>
    /// <returns>The key value as a string, or null if no key accessor is configured.</returns>
    public string? GetKey(T entity)
    {
        if (keyAccessor == null || entity == null)
            return null;

        return keyAccessor(entity);
    }
}


/// <summary>
/// Producer-specific configuration options for the RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerRetryMiddlewareSettings<T> : RetryMiddlewareSettings<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerCircuitBreakerMiddlewareSettings<T> : CircuitBreakerMiddlewareSettings<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerBatchMiddlewareSettings<T> : BatchMiddlewareSettings<T>
    where T : class
{
}
