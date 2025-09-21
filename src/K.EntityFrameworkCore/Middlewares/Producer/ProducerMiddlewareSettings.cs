using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace K.EntityFrameworkCore.Middlewares.Producer;

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

internal class ProducerMiddlewareSettings<T>(ClientSettings<T> clientSettings, IModel model) : MiddlewareSettings<T>(true)
    where T : class
{
    private Func<T, string>? keyAccessor;
    private Dictionary<string, Func<T, object>>? headerAccessors;

    private readonly ProducerConfig producerConfig = new(clientSettings.ClientConfig);

    public IEnumerable<KeyValuePair<string, string>> ProducerConfig => this.producerConfig;

    public string TopicName => clientSettings.TopicName;

    /// <summary>
    /// Gets the key value for the specified entity instance.
    /// </summary>
    /// <param name="entity">The entity instance to extract the key from.</param>
    /// <returns>The key value as a string, or null if configured with no key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no key accessor is configured and no suitable key property is found, or entity is null.</exception>
    public string? GetKey(T entity)
    {
        if (entity == null)
        {
            throw new InvalidOperationException("Entity cannot be null when extracting key.");
        }

        if (model.HasNoKey<T>())
        {
            return null;
        }

        var keyPropertyAccessorExpression = model.GetKeyPropertyAccessor<T>();
        if (keyPropertyAccessorExpression != null)
        {
            if (this.keyAccessor == null)
            {
                var parameter = Expression.Parameter(typeof(T), "entity");
                Expression propertyExpression;
                if (keyPropertyAccessorExpression is LambdaExpression lambda)
                {
                    var parameterReplacer = new ParameterReplacer(lambda.Parameters[0], parameter);
                    propertyExpression = parameterReplacer.Visit(lambda.Body);
                }
                else
                {
                    propertyExpression = keyPropertyAccessorExpression;
                }

                var toStringMethod = typeof(object).GetMethod("ToString")!;
                var toStringCall = Expression.Call(propertyExpression, toStringMethod);
                var lambdaExpression = Expression.Lambda<Func<T, string>>(toStringCall, parameter);
                this.keyAccessor = lambdaExpression.Compile();
            }

            return this.keyAccessor(entity);
        }

        if (this.keyAccessor != null)
        {
            return this.keyAccessor(entity);
        }

        var keyProperty = FindKeyProperty() ?? throw new InvalidOperationException(
            $"No key accessor has been configured and no suitable key property found for type '{typeof(T).Name}'. " +
            $"To resolve this, choose one of the following options:\n" +
            $"1. Decorate a property with [Key] attribute: [Key] public string MyKey {{ get; set; }}\n" +
            $"2. Add a property named 'Id': public string Id {{ get; set; }}\n" +
            $"3. Configure explicitly using HasKey(): .HasKey(x => x.MyProperty)\n" +
            $"4. Use HasNoKey() for random partitioning: .HasNoKey()\n");

        this.keyAccessor = SetKeyAccessorFromProperty(keyProperty);

        return this.keyAccessor(entity);
    }

    /// <summary>
    /// Gets the custom headers for the specified entity instance.
    /// </summary>
    /// <param name="message">The entity instance to extract headers from.</param>
    /// <returns>A dictionary of header key-value pairs.</returns>
    public ImmutableDictionary<string, string> GetHeaders(T message)
    {
        if (message == null)
        {
            throw new InvalidOperationException("Entity cannot be null when extracting headers.");
        }

        var result = new Dictionary<string, string>();

        if (this.headerAccessors == null)
        {
            var headerAccessorExpressions = model.GetHeaderAccessors<T>();

            if (headerAccessorExpressions.Count == 0)
            {
                this.headerAccessors = [];
            }
            else
            {
                this.headerAccessors = [];

                foreach (var headerAccessor in headerAccessorExpressions)
                {
                    string headerKey = headerAccessor.Key;
                    Expression headerValueExpression = headerAccessor.Value;

                    var compiledAccessor = CompileHeaderExpression(headerValueExpression);
                    this.headerAccessors[headerKey] = compiledAccessor;
                }
            }
        }

        foreach (var accessor in this.headerAccessors)
        {
            string headerKey = accessor.Key;
            Func<T, object> headerValueAccessor = accessor.Value;

            object headerValue = headerValueAccessor(message);
            result[headerKey] = headerValue.ToString()!;
        }

        Type messageType = typeof(T);
        result["$type"] = messageType.AssemblyQualifiedName!;

        Type messageRuntimeType = message.GetType();
        if (messageRuntimeType != messageType)
        {
            result["$runtimeType"] = messageRuntimeType.AssemblyQualifiedName!;
        }

        return result.ToImmutableDictionary();
    }

    /// <summary>
    /// Compiles a header value expression into a delegate that can be invoked.
    /// </summary>
    /// <param name="expression">The expression to compile.</param>
    /// <returns>A compiled function that extracts the header value from a message.</returns>
    private static Func<T, object> CompileHeaderExpression(Expression expression)
    {
        if (expression is LambdaExpression lambda)
        {
            // Already a lambda expression, just cast and compile
            return ((Expression<Func<T, object>>)lambda).Compile();
        }

        // Create a lambda expression with parameter
        var parameter = Expression.Parameter(typeof(T), "message");
        var lambdaExpression = Expression.Lambda<Func<T, object>>(expression, parameter);

        return lambdaExpression.Compile();
    }

    /// <summary>
    /// Finds a suitable key property by looking for 'Id' property or KeyAttribute.
    /// </summary>
    /// <returns>The PropertyInfo of the key property, or null if not found.</returns>
    private static PropertyInfo? FindKeyProperty() =>
        typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
            ??
        typeof(T)
            .GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    /// <summary>
    /// Creates and sets the key accessor from a PropertyInfo.
    /// </summary>
    /// <param name="property">The property to create the accessor from.</param>
    private static Func<T, string> SetKeyAccessorFromProperty(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(T), "event");
        var propertyAccess = Expression.Property(parameter, property);
        var toStringMethod = typeof(object).GetMethod("ToString")!;
        var toStringCall = Expression.Call(propertyAccess, toStringMethod);
        var lambdaExpression = Expression.Lambda<Func<T, string>>(toStringCall, parameter);
        return lambdaExpression.Compile();
    }
}


