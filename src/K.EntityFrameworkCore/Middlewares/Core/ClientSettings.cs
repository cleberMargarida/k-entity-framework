using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares.Core;

[SingletonService]
internal class ClientSettings<T>(ClientConfig clientConfig) : MiddlewareSettings<T>
    where T : class
{
    public virtual ClientConfig ClientConfig => clientConfig;

    public string TopicName { get; set; } = typeof(T).IsNested ? typeof(T).FullName!.Replace('+','.') : typeof(T).FullName;
}
