using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;

namespace K.EntityFrameworkCore
{
    /// <summary>
    /// Central service responsible for polling Kafka consumer and distributing messages
    /// to appropriate channels based on message type. This service starts automatically
    /// when first accessed and runs in the background.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the KafkaConsumerPollService.
    /// </remarks>
    /// <param name="serviceProvider">Service provider for resolving dependencies</param>
    /// <param name="consumer">Kafka consumer instance</param>
    public sealed class KafkaConsumerPollService(
        IServiceProvider serviceProvider,
        IConsumer consumer) : IDisposable
    {
        private readonly CancellationTokenSource cts = new();
        private readonly object startLock = new();
        
        private Task? pollTask;
        private bool isStarted;
        private bool disposed;

        /// <summary>
        /// Ensures the poll service is started. This method is thread-safe and can be called multiple times.
        /// </summary>
        public void EnsureStarted()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (isStarted)
            {
                return;
            }

            lock (startLock)
            {
                if (isStarted)
                    return;

                pollTask = Task.Run(async () => await PollLoopAsync(cts.Token));
                isStarted = true;
            }
        }

        private async Task PollLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(cancellationToken);
                        if (result?.Message == null) 
                            continue;

                        // Extract the type information from the message
                        var type = LoadGenericTypeFromConsumeResult(result);
                        if (type == null)
                        {
                            continue;
                        }

                        var channel = serviceProvider.GetKeyedService<IConsumeResultChannel>(type);
                        if (channel == null)
                        {
                            continue;
                        }

                        await channel.WriteAsync(result, cancellationToken);
                    }
                    catch (ConsumeException)
                    {
                        // Continue processing other messages
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Poll loop cancelled
            }
            catch (Exception)
            {
                // Fatal error in poll loop
            }
        }

        private Type? LoadGenericTypeFromConsumeResult(ConsumeResult<string, byte[]> result)
        {
            // Try to get type information from message headers
            if (result.Message.Headers.TryGetLastBytes("$type", out byte[]? typeNameBytes) && typeNameBytes != null)
            {
                string typeName = Encoding.UTF8.GetString(typeNameBytes);
                Type? type = Type.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            // TODO: Implement additional type resolution logic here
            // This could be based on:
            // 1. Topic name patterns
            // 2. Message content inspection
            // 3. Custom type mapping configuration

            // Example implementation based on topic name:
            // return TypeRegistry.GetTypeFromTopicName(result.Topic);

            // For now, return null if no type could be determined
            return null;
        }

        /// <summary>
        /// Disposes the KafkaConsumerPollService and stops the polling loop.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            
            cts.Cancel();
            
            if (pollTask != null)
            {
                try
                {
                    pollTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception)
                {
                    // Error waiting for poll task to complete during disposal
                }
            }
            
            cts.Dispose();
        }
    }
}
