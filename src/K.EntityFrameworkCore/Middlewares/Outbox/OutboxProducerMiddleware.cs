using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Batch;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Outbox
{
    internal class OutboxProducerMiddleware<T>(IServiceProvider serviceProvider, OutboxMiddlewareSettings<T> settings) 
        : Middleware<T>(settings) 
        , IEndMiddleware
        where T : class
    {
        private readonly IBatchProducer producer = serviceProvider.GetRequiredKeyedService<IBatchProducer>(typeof(T));

        public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            var outboxMessage = envelope.AsOutboxMessage();

            producer.Produce(outboxMessage.AggregateId, new Message<string, byte[]>
            {
                Headers = envelope.GetHeaders(),
                Key = outboxMessage.AggregateId!,
                Value = outboxMessage.Payload,

            }, HandleDeliveryReport);

            return ValueTask.CompletedTask;

            void HandleDeliveryReport(DeliveryReport<string, byte[]> report)
            {
                outboxMessage.ProcessedAt = report.Timestamp.UtcDateTime;
                bool isSuccessfullyProcessed = report.Error.Code is ErrorCode.NoError;
                if (isSuccessfullyProcessed)
                {
                    outboxMessage.IsSuccessfullyProcessed = true;
                }
                else
                {
                    outboxMessage.Retries++;
                }
            }
        }
    }
}
