using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddlewareSettings<T>(ClientSettings<T> clientSettings) : MiddlewareSettings<T>(isMiddlewareEnabled: true)
    where T : class
{
    private readonly ConsumerConfig consumerConfig = new(clientSettings.ClientConfig);

    public string GroupId
    {
        get => consumerConfig.GroupId ??= AppDomain.CurrentDomain.FriendlyName;
        set => consumerConfig.GroupId = value;
    }

    public IEnumerable<KeyValuePair<string, string>> ConsumerConfig => consumerConfig;
}


