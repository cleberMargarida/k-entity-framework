﻿using Confluent.Kafka;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Producer;
using System.Text;

namespace K.EntityFrameworkCore.Middlewares.Outbox
{
    internal class OutboxProducerMiddleware<T>(
        IProducer producer,
        OutboxMiddlewareSettings<T> settings,
        ProducerMiddlewareSettings<T> producerMiddlewareSettings)
        : Middleware<T>(settings)
        where T : class
    {
        private readonly string topicName = producerMiddlewareSettings.TopicName;

        public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            envelope.WeakReference.TryGetTarget(out object? target);
            OutboxMessage outboxMessage = target as OutboxMessage ?? throw new InvalidOperationException("Outbox not stored.");

            var tcs = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);

            producer.Produce(this.topicName, new Message<string, byte[]>
            {
                Headers = outboxMessage.Headers.ToConfluentHeaders(),
                Key = outboxMessage.AggregateId!,
                Value = outboxMessage.Payload,

            }, HandleDeliveryReport);


            return new ValueTask<T?>(tcs.Task);

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

                tcs.SetResult(null);
            }
        }
    }
}

internal static class HeadersExtensions
{
    public static Headers ToConfluentHeaders(this IDictionary<string, string> headers) =>
        [..
            headers.Select(kv => new Header(kv.Key, Encoding.UTF8.GetBytes(kv.Value)))
        ];
}