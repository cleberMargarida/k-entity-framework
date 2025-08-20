//using K.EntityFrameworkCore.Interfaces;

//namespace K.EntityFrameworkCore.Extensions;

///// <summary>
///// Extension methods for registering custom serialization strategies.
///// Uses compile-time types - no strings or runtime reflection.
///// </summary>
//public static class SerializationExtensions
//{
//    /// <summary>
//    /// Registers a new serialization strategy using the options type as the unique identifier.
//    /// This allows external libraries to plug in their own serializers without modifying the core.
//    /// </summary>
//    /// <typeparam name="TSerializer">The serializer implementation type (must be generic like MySerializer&lt;&gt;).</typeparam>
//    /// <typeparam name="TOptions">The options type that uniquely identifies this strategy.</typeparam>
//    /// <param name="defaultOptionsFactory">Factory to create default settings.</param>
//    /// <example>
//    /// <code>
//    /// SerializationExtensions.RegisterStrategy&lt;MyCustomSerializer&lt;&gt;, MyCustomOptions&gt;(
//    ///     () => new MyCustomOptions { Setting1 = "default" }
//    /// );
//    /// </code>
//    /// </example>
//    public static void RegisterStrategy<TSerializer, TOptions>(Func<TOptions> defaultOptionsFactory)
//        where TOptions : class, new()
//    {
//        var strategy = new SerializationStrategy<TOptions>
//        {
//            SerializerFactory = options =>
//            {
//                // Create the generic serializer type TSerializer<object>
//                var serializerType = typeof(TSerializer).MakeGenericType(typeof(object));
//                return (IMessageSerializer<object>)Activator.CreateInstance(serializerType, options)!;
//            },
//            DefaultOptionsFactory = defaultOptionsFactory
//        };

//        SerializationStrategyRegistry.RegisterStrategy(strategy);
//    }
//}
