using Confluent.Kafka;
using System.Linq.Expressions;

namespace K.EntityFrameworkCore.MiddlewareOptions.Producer;

/// <summary>
/// Helper class to replace parameter expressions in expression trees.
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}


internal class ProducerMiddlewareOptions<T>(ClientOptions<T> clientOptions) : MiddlewareOptions<T>(true)
    where T : class
{
    private Func<T, string>? keyAccessor;

    private readonly ProducerConfig producerConfig = new(clientOptions.ClientConfig);
    public IEnumerable<KeyValuePair<string, string>> ProducerConfig => producerConfig;
    public string TopicName => clientOptions.TopicName;

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
public class ProducerRetryMiddlewareOptions<T> : RetryMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerCircuitBreakerMiddlewareOptions<T> : CircuitBreakerMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerBatchMiddlewareOptions<T> : BatchMiddlewareOptions<T>
    where T : class
{
}
