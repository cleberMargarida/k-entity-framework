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
            var initializer = _topicInitializers.GetOrAdd(contextType, CreateTopicInitializer);
            initializer(context);
        }

        private static Action<DbContext> CreateTopicInitializer(Type contextType)
        {
            var contextParam = Expression.Parameter(typeof(DbContext), "context");
            var typedContextVar = Expression.Variable(contextType, "typedContext");
            var typedContextAssign = Expression.Assign(typedContextVar, Expression.Convert(contextParam, contextType));

            var statements = new List<Expression> { typedContextAssign };

            var topicProperties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => IsTopicProperty(prop.PropertyType));

            foreach (var property in topicProperties)
            {
                var topicType = property.PropertyType;
                var entityType = topicType.GetGenericArguments()[0];
                var constructor = topicType.GetConstructor([typeof(DbContext)]);
                var newTopicExpr = Expression.New(constructor!, typedContextVar);
                var propertyAccess = Expression.Property(typedContextVar, property);
                var assignExpr = Expression.Assign(propertyAccess, newTopicExpr);
                statements.Add(assignExpr);
            }

            if (statements.Count == 1)
            {
                return static _ => { };
            }

            var body = Expression.Block([typedContextVar], statements);
            var lambda = Expression.Lambda<Action<DbContext>>(body, contextParam);
            return lambda.Compile();
        }

        private static bool IsTopicProperty(Type propertyType)
        {
            return propertyType.IsGenericType && 
                   propertyType.GetGenericTypeDefinition() == typeof(Topic<>);
        }
    }

}
