namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Producer interface for sending messages of type <typeparamref name="T"/>.
/// </summary>
public interface IProducer<T>
    where T : class
{
    /// <summary>
    /// Produces a domain event to the Kafka topic.
    /// </summary>
    void Produce(T message);
}
