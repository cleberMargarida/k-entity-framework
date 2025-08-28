using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Producer;

namespace K.EntityFrameworkCore.Middlewares.Outbox
{
    [SingletonService]
    internal class OutboxProducerMiddleware<T>(
        IProducer producer, 
        OutboxMiddlewareSettings<T> settings,
        ProducerMiddlewareSettings<T> producerMiddlewareSettings) 
        : Middleware<T>(settings) 
        where T : class
    {
        private readonly string topicName = producerMiddlewareSettings.TopicName;

        public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            var outboxMessage = envelope.AsOutboxMessage();

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            producer.Produce(topicName, new Message<string, byte[]>
            {
                Headers = envelope.GetHeaders(),
                Key = outboxMessage.AggregateId!,
                Value = outboxMessage.Payload,

            }, HandleDeliveryReport);


            return new ValueTask(tcs.Task);

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

                tcs.SetResult();
            }
        }
    }
}
