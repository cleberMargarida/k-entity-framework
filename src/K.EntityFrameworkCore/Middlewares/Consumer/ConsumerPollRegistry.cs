using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class ConsumerPollRegistry(ILogger<ConsumerPollRegistry> logger, IServiceProvider serviceProvider) : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly HashSet<IConsumer> consumers = [];
    private readonly CancellationToken cancellationToken;

    public ConsumerPollRegistry(ILogger<ConsumerPollRegistry> logger, IServiceProvider serviceProvider, IConsumer globalConsumer) : this(logger, serviceProvider)
    {
        this.cancellationToken = this.cancellationTokenSource.Token;

        Register<Null>(globalConsumer);
    }

    public async void Register<T>(IConsumer<string, byte[]> consumer)
        where T : class
    {
        lock (this.consumers)
        {
            if (!this.consumers.Add(consumer))
                return;
        }

        await Task.Yield();

        try
        {
            while (!this.cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(this.cancellationToken);
                    if (result?.Message == null)
                    {
                        continue;
                    }

                    var channel = GetChannel<T>(result);

                    await channel.WriteAsync(result, this.cancellationToken);
                }
                catch (ConsumeException ex) when (ex.ConsumerRecord?.TopicPartitionOffset is { Offset.IsSpecial: true })
                {
                    logger.LogWarning(ex, "Consume error occurred. Error: {Error}, Topic: {Topic}, Partition: {Partition}", ex.Error.Reason, ex.ConsumerRecord.Topic, ex.ConsumerRecord.Partition);
                    continue;
                }
                catch (ConsumeException ex) when (ex.ConsumerRecord?.TopicPartitionOffset != null)
                {
                    logger.LogWarning(ex, "Consume error, seeking back to offset. Error: {Error}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", ex.Error.Reason, ex.ConsumerRecord.Topic, ex.ConsumerRecord.Partition, ex.ConsumerRecord.Offset);
                    consumer.Seek(ex.ConsumerRecord.TopicPartitionOffset);
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning(ex, "An local consume error occurred. No messages lost, consumption continues. Error: {Error}", ex.Error.Reason);
                }
                catch (OperationCanceledException) when (this.cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("The consumer polling has been cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error while consuming type {Type}: {Message}. Stopping consumer.", typeof(T).Name, ex.Message);
                    return;
                }
            }
        }
        finally
        {
            if (typeof(T) != typeof(Null))
            {
                consumer.Dispose();
            }
        }
    }

    private Channel GetChannel<T>(ConsumeResult<string, byte[]> result) where T : class
    {
        if (typeof(T) == typeof(Null))
        {
            return (Channel)serviceProvider.GetRequiredService(typeof(Channel<>).MakeGenericType(LoadGenericTypeFromConsumeResult(result)));
        }
            
        return serviceProvider.GetRequiredService<Channel<T>>();
    }

    private static Type LoadGenericTypeFromConsumeResult(ConsumeResult<string, byte[]> result)
    {
        if (result.Message.Headers == null)
            throw new InvalidOperationException("The message headers are missing; cannot determine the message type.");

        string? typeName = null;

        if (result.Message.Headers.TryGetLastBytes("$runtimeType", out byte[] runtimeTypeBytes))
            typeName = Encoding.UTF8.GetString(runtimeTypeBytes);
        else if (result.Message.Headers.TryGetLastBytes("$type", out byte[] typeNameBytes))
            typeName = Encoding.UTF8.GetString(typeNameBytes);

        if (typeName == null)
            throw new InvalidOperationException("The required '$runtimeType' or '$type' Kafka header was not found on the message.");

        return Type.GetType(typeName) ?? throw new InvalidOperationException($"The supplied type '{typeName}' could not be loaded from the current running assemblies.");
    }

    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
        this.cancellationTokenSource.Dispose();
    }
}