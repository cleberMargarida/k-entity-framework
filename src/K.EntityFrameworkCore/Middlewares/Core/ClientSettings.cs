using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal interface IClientSettings
{
    ClientConfig ClientConfig { get; }
    string TopicName { get; set; }
}

internal class ClientSettings<T>(ClientConfig clientConfig, ICurrentDbContext currentDbContext) : MiddlewareSettings<T>, IClientSettings where T : class
{
    public virtual ClientConfig ClientConfig => clientConfig;
    private readonly ICurrentDbContext currentDbContext = currentDbContext;

    public string TopicName
    {
        get
        {
            return this.currentDbContext.Context.Model.GetTopicName<T>() ?? (typeof(T).IsNested ? typeof(T).FullName!.Replace('+', '.') : typeof(T).FullName!);
        }
        set
        {
            ((IMutableModel)this.currentDbContext.Context.Model).SetTopicName<T>(value);
        }
    }
}
