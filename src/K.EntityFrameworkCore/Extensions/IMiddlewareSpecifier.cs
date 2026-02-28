
namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Defines the non-generic contract for processing deferred outbox messages.
    /// This interface allows direct dispatch without reflection overhead.
    /// </summary>
    public interface IMiddlewareSpecifier
    {
        /// <summary>
        /// Determines whether this specifier can handle the given outbox message type.
        /// </summary>
        /// <param name="outboxMessage">The outbox message to check.</param>
        /// <returns><c>true</c> if the message type is recognized; otherwise, <c>false</c>.</returns>
        bool CanHandle(OutboxMessage outboxMessage);

        /// <summary>
        /// Executes the middleware pipeline for the specified outbox message.
        /// </summary>
        /// <param name="outboxMessage">The outbox message to process.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        ValueTask ExecuteAsync(OutboxMessage outboxMessage, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines the contract for processing deferred outbox messages for a specific DbContext type.
    /// This interface is implemented by source-generated code to handle type-specific message processing.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
    public interface IMiddlewareSpecifier<T> : IMiddlewareSpecifier
    {
    }
}