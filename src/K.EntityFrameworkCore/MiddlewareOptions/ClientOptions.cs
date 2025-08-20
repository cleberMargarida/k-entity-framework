using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.MiddlewareOptions;

internal class ClientOptions<T>(ClientConfig clientConfig) : MiddlewareOptions<T>
    where T : class
{
    public virtual ClientConfig ClientConfig => clientConfig;

    public string TopicName { get; set; } = typeof(T).FullName ?? typeof(T).Name;
}
