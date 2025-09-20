using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

internal class SubscriberMiddleware<T>(ConsumerAssessor<T> consumerAssessor, ClientSettings<T> clientSettings) : Middleware<T>, IDisposable
    where T : class
{
    private readonly IConsumer consumer = consumerAssessor.Consumer;
    private DisposableActivationToken? activationToken;

    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        if (this.activationToken == null)
        {
            lock (this.consumer.Handle)
            {
                var assignments = this.consumer.Assignment.Select(tp => tp.Topic).ToHashSet();
                assignments.Add(clientSettings.TopicName);
                this.consumer.Subscribe(assignments);
                this.activationToken = new DisposableActivationToken(this.consumer, clientSettings.TopicName);
            }
        }

        return base.InvokeAsync(envelope, cancellationToken);
    }

    public void Dispose()
    {
        this.activationToken?.Dispose();
    }

    private struct DisposableActivationToken(IConsumer consumer, string topicName) : IDisposable
    {
        public readonly void Dispose()
        {
            lock (consumer.Handle)
            {
                var assignments = consumer.Assignment.Select(tp => tp.Topic).ToHashSet();
                assignments.Remove(topicName);

                if (assignments.Count == 0) consumer.Unsubscribe();
                else consumer.Subscribe(assignments);
            }
        }
    }
}
