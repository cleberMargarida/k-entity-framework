using System.Collections.Concurrent;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// A singleton cache for storing and retrieving middleware options by type.
/// </summary>
internal static class ServiceProviderCache
{
    private static readonly ConcurrentDictionary<Type, object> _cache = new();

    /// <summary>
    /// Gets the singleton instance of the cache.
    /// </summary>
    public static ServiceProviderCache Instance => new();

    /// <summary>
    /// Gets or adds a value to the cache for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to use as the key.</typeparam>
    /// <param name="key">The type key.</param>
    /// <param name="valueFactory">Factory function to create the value if it doesn't exist.</param>
    /// <returns>The cached or newly created value.</returns>
    public T GetOrAdd<T>(Type key, Func<Type, T> valueFactory)
    {
        return (T)_cache.GetOrAdd(key, k => valueFactory(k)!);
    }

    /// <summary>
    /// Gets or adds a value to the cache using the type T as the key.
    /// </summary>
    /// <typeparam name="T">The type to use as both key and value type.</typeparam>
    /// <param name="valueFactory">Factory function to create the value if it doesn't exist.</param>
    /// <returns>The cached or newly created value.</returns>
    public T GetOrAdd<T>(Func<T> valueFactory) where T : class
    {
        return (T)_cache.GetOrAdd(typeof(T), _ => valueFactory());
    }

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
}
