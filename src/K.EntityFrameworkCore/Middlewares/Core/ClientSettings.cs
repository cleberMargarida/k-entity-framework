using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal interface IClientSettings
{
    ClientConfig ClientConfig { get; }
    string TopicName { get; }
}

internal class ClientSettings<T>(ClientConfig clientConfig, ICurrentDbContext currentDbContext) : MiddlewareSettings<T>, IClientSettings where T : class
{
    public virtual ClientConfig ClientConfig => clientConfig;
    private readonly ICurrentDbContext currentDbContext = currentDbContext;

    [field: AllowNull]
    public string TopicName
    {
        get
        {
            field ??= this.currentDbContext.Context.Model.GetTopicName<T>()
                  ?? throw new InvalidOperationException(
                      $"The type '{typeof(T).Name}' is not configured. " +
                       "Did you register it using modelBuilder.Topic<OrderEvent>(topic => ...) in OnModelCreating()");

            return field;
        }
    }
}
