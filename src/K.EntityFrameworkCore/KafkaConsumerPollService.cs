using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Extensions;
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
    /// <param name="consumerFactory">Factory to lazily create a Kafka consumer instance</param>
    [SingletonService]
    public sealed class KafkaConsumerPollService(
        IServiceProvider serviceProvider,
        Func<IConsumer> consumerFactory) : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock startLock = new();
#else
        private readonly object startLock = new();
#endif
        private CancellationTokenSource? cts = null;
        private readonly Func<IConsumer> consumerFactory = consumerFactory;
        private IConsumer? consumer;

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

                // Lazy-create the consumer only when we actually start polling
                consumer ??= consumerFactory();

                cts = new CancellationTokenSource();
                pollTask = PollLoopAsync(cts.Token);
                isStarted = true;
            }
        }

        /// <summary>
        /// Stops the polling loop if running and disposes the underlying consumer. Safe to call multiple times.
        /// On subsequent EnsureStarted(), a new consumer will be created lazily.
        /// </summary>
        public void Stop()
        {
            lock (startLock)
            {
                if (!isStarted)
                    return;

                try
                {
                    cts?.Cancel();
                    pollTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch
                {
                    // swallow stop exceptions
                }
                finally
                {
                    try
                    {
                        consumer?.Close();
                        consumer?.Dispose();
                    }
                    catch
                    {
                        // ignore dispose failures
                    }

                    consumer = null;
                    pollTask = null;
                    cts?.Dispose();
                    cts = null;
                    isStarted = false;
                }
            }
        }

        private async Task PollLoopAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer!.Consume(cancellationToken);
                        if (result?.Message == null)
                            continue;

                        // Extract the type information from the message
                        var type = LoadGenericTypeFromConsumeResult(result);
                        if (type == null)
                            continue;

                        var channel = serviceProvider.GetKeyedService<IConsumeResultChannel>(type);
                        if (channel == null)
                            continue;

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
        }

        private static Type? LoadGenericTypeFromConsumeResult(ConsumeResult<string, byte[]> result)
        {
            result.Message.Headers.TryGetLastBytes("$type", out byte[] typeNameBytes);

            string typeName = Encoding.UTF8.GetString(typeNameBytes);

            Type? otherType = Type.GetType(typeName) ?? throw new InvalidOperationException($"The supplied type {typeName} could not be loaded from the current running assemblies.");

            return otherType;
        }

        /// <summary>
        /// Disposes the KafkaConsumerPollService and stops the polling loop.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            try
            {
                Stop();
            }
            catch
            {
                // ignore dispose-time stop errors
            }
        }
    }
}
