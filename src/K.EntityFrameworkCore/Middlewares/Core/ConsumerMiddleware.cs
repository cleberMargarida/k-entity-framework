using Confluent.Kafka;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public TaskCompletionSource<ConsumeResult<string, byte[]>> TaskCompletionSource { get; private set; }
        = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // here consumer can/will consume from another {T}.
        var result = consumer.Consume(cancellationToken);

        result.Message.Headers.TryGetLastBytes("$type", out byte[] bytes);

        string typeName = Encoding.UTF8.GetString(bytes);

        Type? otherType = Type.GetType(typeName) 
            ?? throw new InvalidOperationException($"The supplied type {typeName} could not be loaded from the current running assemblies.");

        ISerializedEnvelope<T> serializedEnvelope = envelope;
        if (typeof(T).IsAssignableFrom(otherType))
        {
            this.TaskCompletionSource.SetResult(result);
        }
        else
        {
            var other = provider.GetRequiredKeyedService<IConsumeResultSource>(otherType);
            other.TaskCompletionSource.SetResult(result);
        }

        result = await TaskCompletionSource.Task;
        TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        serializedEnvelope.Headers = result.Message.Headers.ToDictionary(h => h.Key, h => (object)h.GetValueBytes());
        serializedEnvelope.Key = result.Message.Key;
        serializedEnvelope.SerializedData = result.Message.Value;

        await base.InvokeAsync(envelope, cancellationToken);
    }
}

internal interface IConsumeResultSource
{
    TaskCompletionSource<ConsumeResult<string, byte[]>> TaskCompletionSource { get; }
}