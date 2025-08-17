using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.MiddlewareOptions;

internal class ClientOptions<T>(Infrastructure<ClientConfig> clientConfig) : MiddlewareOptions<T>
    where T : class
{
    public virtual ClientConfig ClientConfig => clientConfig.Instance;

    public string TopicName { get; set; } = typeof(T).FullName ?? typeof(T).Name;
}
