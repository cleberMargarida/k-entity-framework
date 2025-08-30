using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

[ScopedService]
internal class ProducerMiddlewareSettings<T>(ClientSettings<T> clientSettings, IServiceProvider serviceProvider) : MiddlewareSettings<T>(true)
    where T : class
{
    private Func<T, string>? keyAccessor;
    private bool hasNoKey = false;
    private Lazy<DbContext> dbContext = new(() => serviceProvider.GetRequiredService<ICurrentDbContext>().Context);

    private readonly ProducerConfig producerConfig = new(clientSettings.ClientConfig);

    public IEnumerable<KeyValuePair<string, string>> ProducerConfig => producerConfig;

    public string TopicName => dbContext.Value.Model.GetTopicName<T>() ?? 
        clientSettings.TopicName;

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

        // Check model annotations first
        var model = this.dbContext.Value.Model;
        
        // Check if configured to have no key
        if (model.HasNoKey<T>())
        {
            return null;
        }

        // Check if there's a key property accessor in the model annotations
        var keyPropertyAccessorExpression = model.GetKeyPropertyAccessor<T>();
        if (keyPropertyAccessorExpression != null)
        {
            // Compile the expression if we haven't already
            if (keyAccessor == null)
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
                keyAccessor = lambdaExpression.Compile();
            }
            
            return keyAccessor(entity);
        }

        // If explicitly configured to have no key (legacy check), return null
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


