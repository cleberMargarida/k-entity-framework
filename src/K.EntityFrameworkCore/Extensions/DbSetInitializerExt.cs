using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
#pragma warning disable IDE0079
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    internal class DbSetInitializerExt(IDbSetFinder setFinder, IDbSetSource setSource) : DbSetInitializer(setFinder, setSource)
    {
        // Cache compiled initialization delegates per context type for performance
        private static readonly ConcurrentDictionary<Type, Action<DbContext>> _topicInitializers = new();

        public override void InitializeSets(DbContext context)
        {
            // Initialize standard EF Core DbSets first
            base.InitializeSets(context);
            
            // Initialize Kafka Topics using compiled expressions (CLR strategy)
            InitializeTopicSets(context);
        }

        private static void InitializeTopicSets(DbContext context)
        {
            var contextType = context.GetType();
            
            // Get or create cached compiled initializer for this context type
            var initializer = _topicInitializers.GetOrAdd(contextType, CreateTopicInitializer);
            
            // Execute the compiled initializer
            initializer(context);
        }

        private static Action<DbContext> CreateTopicInitializer(Type contextType)
        {
            var contextParam = Expression.Parameter(typeof(DbContext), "context");
            var typedContextVar = Expression.Variable(contextType, "typedContext");
            var typedContextAssign = Expression.Assign(typedContextVar, Expression.Convert(contextParam, contextType));

            var statements = new List<Expression> { typedContextAssign };

            // Get all public properties that are of type Topic<T>
            var topicProperties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => IsTopicProperty(prop.PropertyType));

            foreach (var property in topicProperties)
            {
                // Create compiled expression to initialize each Topic<T> property
                var topicType = property.PropertyType;
                var entityType = topicType.GetGenericArguments()[0];

                // Create constructor call: new Topic<T>(dbContext)
                var constructor = topicType.GetConstructor([typeof(DbContext)]);
                if (constructor == null)
                {
                    throw new InvalidOperationException($"Topic type {topicType.Name} must have a constructor that accepts DbContext.");
                }
                
                var newTopicExpr = Expression.New(constructor, typedContextVar);
                
                // Create property assignment: context.PropertyName = new Topic<T>(context)
                var propertyAccess = Expression.Property(typedContextVar, property);
                var assignExpr = Expression.Assign(propertyAccess, newTopicExpr);
                
                statements.Add(assignExpr);
            }

            // If no topic properties found, return empty action
            if (statements.Count == 1)
            {
                return _ => { };
            }

            // Create block expression with all assignments
            var body = Expression.Block(new[] { typedContextVar }, statements);
            
            // Compile to delegate
            var lambda = Expression.Lambda<Action<DbContext>>(body, contextParam);
            return lambda.Compile();
        }

        private static bool IsTopicProperty(Type propertyType)
        {
            // Check if the property type is a generic type and if it's Topic<T>
            return propertyType.IsGenericType && 
                   propertyType.GetGenericTypeDefinition() == typeof(Topic<>);
        }
    }

}
