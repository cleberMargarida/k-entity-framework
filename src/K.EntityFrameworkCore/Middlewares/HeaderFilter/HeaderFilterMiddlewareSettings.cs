using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Middlewares.HeaderFilter;

/// <summary>
/// Settings for configuring header filter middleware behavior.
/// Stores header key-value pairs that must match for message processing.
/// </summary>
/// <typeparam name="T">The message type being processed.</typeparam>
internal class HeaderFilterMiddlewareSettings<T>(IModel model) : MiddlewareSettings<T>(model.IsHeaderFilterEnabled<T>())
    where T : class
{
    public Dictionary<string, string> HeaderFilters { get; } = model.GetHeaderFilters<T>();
}
