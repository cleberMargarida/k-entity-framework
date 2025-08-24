using Confluent.Kafka;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ClientSettings<T>(ClientConfig clientConfig) : MiddlewareSettings<T>
    where T : class
{
    public virtual ClientConfig ClientConfig => clientConfig;

    public string TopicName { get; set; } = typeof(T).FullName ?? typeof(T).Name;
}
