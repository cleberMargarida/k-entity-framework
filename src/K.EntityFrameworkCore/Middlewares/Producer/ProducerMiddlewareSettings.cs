using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;

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

[SingletonService]
internal class ProducerMiddlewareSettings<T>(ClientSettings<T> clientSettings) : MiddlewareSettings<T>(true)
    where T : class
{
    private Func<T, string>? keyAccessor;
    private bool hasNoKey = false;

    private readonly ProducerConfig producerConfig = new(clientSettings.ClientConfig);
    public IEnumerable<KeyValuePair<string, string>> ProducerConfig => producerConfig;

    [field: AllowNull]
    public string TopicName => field ??= clientSettings.TopicName;

    public Expression KeyPropertyAccessor
    {
        set
        {
            hasNoKey = false; // Reset the no key flag when setting a new accessor

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
    /// Configures the producer to have no key (returns null for all messages).
    /// This disables automatic key discovery and key-based partitioning.
    /// </summary>
    public void SetNoKey()
    {
        hasNoKey = true;
        keyAccessor = null;
    }

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

        // If explicitly configured to have no key, return null
        if (hasNoKey)
        {
            return null;
        }

        if (keyAccessor != null)
        {
            return keyAccessor(entity);
        }

        var keyProperty = FindKeyProperty() ?? throw new InvalidOperationException(
            $"No key accessor has been configured and no suitable key property found for type '{typeof(T).Name}'. " +
            $"To resolve this, choose one of the following options:\n" +
            $"1. Decorate a property with [Key] attribute: [Key] public string MyKey {{ get; set; }}\n" +
            $"2. Add a property named 'Id': public string Id {{ get; set; }}\n" +
            $"3. Configure explicitly using HasKey(): .HasKey(x => x.MyProperty)\n" +
            $"4. Use HasNoKey() for random partitioning: .HasNoKey()\n");

        keyAccessor = SetKeyAccessorFromProperty(keyProperty);

        return keyAccessor(entity);
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


