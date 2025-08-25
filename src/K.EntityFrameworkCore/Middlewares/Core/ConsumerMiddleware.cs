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
        ConsumeResult<string, byte[]>? result = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            // here consumer can/will consume from another {T}.
            result = consumer.Consume(cancellationToken);

            if (result is not null and { Message: not null })
                break;

            if (cancellationToken.IsCancellationRequested)
                return;
        }

        if (result is null or { Message: null })
        {
            return;
        }

        var otherType = LoadGenericType(result);
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

        FillEnvelopeWithConsumeResult(envelope, result);

        await base.InvokeAsync(envelope, cancellationToken);
    }

    private static void FillEnvelopeWithConsumeResult(ISerializedEnvelope<T> envelope, ConsumeResult<string, byte[]> result)
    {
        envelope.Headers = result.Message.Headers.ToDictionary(h => h.Key, h => (object)h.GetValueBytes());
        envelope.Key = result.Message.Key;
        envelope.SerializedData = result.Message.Value;
    }

    private static Type LoadGenericType(ConsumeResult<string, byte[]> result)
    {
        result.Message.Headers.TryGetLastBytes("$type", out byte[] typeNameBytes);

        string typeName = Encoding.UTF8.GetString(typeNameBytes);

        Type? otherType = Type.GetType(typeName) ?? throw new InvalidOperationException($"The supplied type {typeName} could not be loaded from the current running assemblies.");

        return otherType;
    }

    public void SetResult(ConsumeResult<string, byte[]> result)
    {
        lock (sync)
        {
            // Check if the TaskCompletionSource is still valid and hasn't been completed yet
            TaskCompletionSource.SetResult(result);
        }
    }
}

interface IConsumeResultSource
{
    void SetResult(ConsumeResult<string, byte[]> result);
}