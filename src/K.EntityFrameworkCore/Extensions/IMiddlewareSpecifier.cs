
namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Defines the contract for processing deferred outbox messages for a specific DbContext type.
    /// This interface is implemented by source-generated code to handle type-specific message processing.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
    public interface IMiddlewareSpecifier<T>
    {
        /// <summary>
        /// Creates a deferred execution command for processing the specified outbox message.
        /// </summary>
        /// <param name="outboxMessage">The outbox message to process.</param>
        /// <returns>A scoped command that can be executed later, or null if the message type is not supported.</returns>
        ScopedCommand? DeferedExecution(OutboxMessage outboxMessage);
    }
}