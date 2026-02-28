using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;
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

        bool isPaused = false;
        bool isCircuitBreakerPaused = false;
        HashSet<Channel> pausedChannels = [];

        // Resolve optional circuit breaker from model annotations
        ConsumerCircuitBreaker? circuitBreaker = ResolveCircuitBreaker<T>();

        try
        {
            while (!this.cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Circuit breaker open: pause consumer and wait for reset interval
                    if (circuitBreaker != null && !circuitBreaker.AllowRequest())
                    {
                        if (!isCircuitBreakerPaused)
                        {
                            if (!isPaused)
                            {
                                consumer.Pause(consumer.Assignment);
                            }
                            isCircuitBreakerPaused = true;
                            logger.LogWarning("Consumer paused: circuit breaker is open for type {Type}.", typeof(T).Name);
                        }

                        // Keep heartbeat alive with short timeout consume
                        consumer.Consume(TimeSpan.FromMilliseconds(100));
                        await Task.Delay(100, this.cancellationToken);
                        continue;
                    }

                    // Circuit breaker transitioned to half-open or closed: resume if we were paused by it
                    if (isCircuitBreakerPaused)
                    {
                        isCircuitBreakerPaused = false;
                        if (!isPaused)
                        {
                            consumer.Resume(consumer.Assignment);
                        }
                        logger.LogInformation("Consumer resumed: circuit breaker is now {State} for type {Type}.", circuitBreaker!.State, typeof(T).Name);
                    }

                    // When paused by backpressure, check if all tracked channels have drained below low water mark
                    if (isPaused)
                    {
                        if (pausedChannels.Count > 0 && pausedChannels.All(c => c.ShouldResume))
                        {
                            consumer.Resume(consumer.Assignment);
                            isPaused = false;
                            pausedChannels.Clear();
                            logger.LogInformation("Consumer resumed: all channels below low water mark.");
                        }
                        else
                        {
                            // Keep heartbeat alive with short timeout consume
                            consumer.Consume(TimeSpan.FromMilliseconds(100));
                            await Task.Delay(100, this.cancellationToken);
                            continue;
                        }
                    }

                    var result = consumer.Consume(this.cancellationToken);
                    if (result?.Message == null)
                    {
                        continue;
                    }

                    var channel = GetChannel<T>(result);

                    await channel.WriteAsync(result, this.cancellationToken);

                    // Record success for circuit breaker
                    circuitBreaker?.RecordSuccess();

                    // Check if channel has reached high water mark
                    if (channel.ShouldPause)
                    {
                        pausedChannels.Add(channel);
                        if (!isPaused)
                        {
                            consumer.Pause(consumer.Assignment);
                            isPaused = true;
                            logger.LogInformation("Consumer paused: channel reached high water mark.");
                        }
                    }
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
                    // Record failure for circuit breaker
                    if (circuitBreaker != null)
                    {
                        circuitBreaker.RecordFailure();
                        logger.LogError(ex, "Unexpected error while consuming type {Type}: {Message}. Circuit breaker state: {State}.", typeof(T).Name, ex.Message, circuitBreaker.State);
                        continue;
                    }

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

    private ConsumerCircuitBreaker? ResolveCircuitBreaker<T>() where T : class
    {
        if (typeof(T) == typeof(Null))
        {
            return null;
        }

        var model = serviceProvider.GetService<IModel>();
        var config = model?.GetCircuitBreakerConfig<T>();

        return config != null ? new ConsumerCircuitBreaker(config) : null;
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
        result.Message.Headers.TryGetLastBytes("$type", out byte[] typeNameBytes);

        string typeName = Encoding.UTF8.GetString(typeNameBytes);

        Type otherType = Type.GetType(typeName) ?? throw new InvalidOperationException($"The supplied type {typeName} could not be loaded from the current running assemblies.");

        return otherType;
    }

    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
        this.cancellationTokenSource.Dispose();
    }
}