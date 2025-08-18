using K.EntityFrameworkCore.MiddlewareOptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Producer;

internal class OutboxMiddleware<T>(OutboxMiddlewareOptions<T> outbox, ICurrentDbContext dbContext) : Middleware<T>(outbox)
    where T : class
{
    private readonly DbContext context = dbContext.Context;

    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        ISerializedEnvelope<T> envelopeSerialized = envelope;

        DbSet<OutboxMessage> outboxMessages = context.Set<OutboxMessage>();

        Dictionary<string, string> headers = envelopeSerialized.Headers.ToDictionary(
            h => h.Key,
            h => Convert.ToBase64String((byte[])h.Value));

        OutboxMessage message = new()
        {
            Id = Guid.NewGuid(),
            EventType = typeof(T).AssemblyQualifiedName!,
            Payload = envelopeSerialized.SerializedData,
            Headers = headers.Count > 0 ? JsonSerializer.Serialize(headers) : null,
        };

        outboxMessages.Add(message);

        return outbox.Strategy switch
        {
            OutboxPublishingStrategy.ImmediateWithFallback => base.InvokeAsync(envelope, cancellationToken),
            OutboxPublishingStrategy.BackgroundOnly => ValueTask.CompletedTask,
            _ => throw new NotSupportedException($"The outbox strategy '{outbox.Strategy}' is not supported.")
        };
    }
}
