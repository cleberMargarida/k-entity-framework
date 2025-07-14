using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace KEntityFramework;

public abstract class BrokerContext
{
    protected abstract void OnModelCreating(BrokerModelBuilder model);
    protected abstract void OnConfiguring(BrokerOptionsBuilder options);

    /// <inheritdoc/>
    public virtual void Produce(object message, IEnumerable<KeyValuePair<string, string>>? headers = default)
    {
        _ = nameof(Confluent.Kafka.IProducer<string, string>.Produce);
        throw new NotImplementedException("This method is not implemented. Use a producer to send messages to the topic.");
    }

    /// <inheritdoc/>
    public virtual Task ProduceAsync(object message, IEnumerable<KeyValuePair<string, string>>? headers = default, CancellationToken cancellationToken = default)
    {
        _ = nameof(Confluent.Kafka.IProducer<string, string>.ProduceAsync);
        throw new NotImplementedException("This method is not implemented. Use a producer to send messages to the topic.");
    }

    public virtual void Flush(CancellationToken cancellationToken = default)
    {
        _ = nameof(Confluent.Kafka.IProducer<string, string>.Flush);
        throw new NotImplementedException("This method is not implemented. Use a producer to save changes to the broker.");
    }

    public virtual Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _ = nameof(Confluent.Kafka.IProducer<string, string>.Flush);
        throw new NotImplementedException("This method is not implemented. Use a producer to save changes to the broker.");
    }
}

