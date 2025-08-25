using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal class ConsumerMiddleware<T>(
      IConsumer consumer
    , IServiceProvider provider
    , ConsumerMiddlewareSettings<T> settings)
    : Middleware<T>(settings)
    , IConsumeResultSource
    where T : class
{

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock sync = new();
#else
    private readonly object sync = new();
#endif
    public TaskCompletionSource<ConsumeResult<string, byte[]>> TaskCompletionSource { get; private set; }
        = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // here consumer can/will consume from another {T}.
        var result = consumer.Consume(cancellationToken);

        if (result is null or { Message: null })
        {
            return;
        }

        result.Message.Headers.TryGetLastBytes("$type", out byte[] typeNameBytes);

        string typeName = Encoding.UTF8.GetString(typeNameBytes);

        Type? otherType = Type.GetType(typeName) ?? throw new InvalidOperationException($"The supplied type {typeName} could not be loaded from the current running assemblies.");

        if (typeof(T).IsAssignableFrom(otherType))
        {
            this.SetResult(result);
        }
        else
        {
            var other = provider.GetRequiredKeyedService<IConsumeResultSource>(otherType);
            other.SetResult(result);
        }

        TaskCompletionSource<ConsumeResult<string, byte[]>> currentTcs;

        lock (sync)
        {
            currentTcs = TaskCompletionSource;
        }

        result = await currentTcs.Task;

        lock (sync)
        {
            // Only recreate if this is still the same TaskCompletionSource we were waiting on
            if (TaskCompletionSource == currentTcs)
            {
                TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        ((ISerializedEnvelope<T>)envelope).Headers = result.Message.Headers.ToDictionary(h => h.Key, h => (object)h.GetValueBytes());
        ((ISerializedEnvelope<T>)envelope).Key = result.Message.Key;
        ((ISerializedEnvelope<T>)envelope).SerializedData = result.Message.Value;

        await base.InvokeAsync(envelope, cancellationToken);
    }

    public void SetResult(ConsumeResult<string, byte[]> result)
    {
        lock (sync)
        {
            // Check if the TaskCompletionSource is still valid and hasn't been completed yet
            TaskCompletionSource.TrySetResult(result);
        }
    }
}

internal interface IConsumeResultSource
{
    void SetResult(ConsumeResult<string, byte[]> result);
}