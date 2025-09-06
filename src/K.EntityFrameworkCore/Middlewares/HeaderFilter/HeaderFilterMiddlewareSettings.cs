using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace K.EntityFrameworkCore.Middlewares.HeaderFilter;

/// <summary>
/// Settings for configuring header filter middleware behavior.
/// Stores header key-value pairs that must match for message processing.
/// </summary>
/// <typeparam name="T">The message type being processed.</typeparam>
[ScopedService]
internal class HeaderFilterMiddlewareSettings<T>(IModel model) : MiddlewareSettings<T>(model.IsHeaderFilterEnabled<T>())
    where T : class
{
    public Dictionary<string, string> HeaderFilters { get; } = model.GetHeaderFilters<T>();
}
