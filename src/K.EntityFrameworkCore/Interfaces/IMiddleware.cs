using Microsoft.Extensions.Options;

namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Represents a middleware component that processes messages of type <typeparamref name="T"/>
/// within the producer or consumer pipeline.
/// <para>
/// This is the public extensibility point for the middleware pipeline. Implement this interface
/// to create custom middleware that participates in message processing. Custom middleware is
/// registered via <c>ProducerBuilder&lt;T&gt;.HasMiddleware&lt;TMiddleware&gt;()</c> or
/// <c>ConsumerBuilder&lt;T&gt;.HasMiddleware&lt;TMiddleware&gt;()</c> and is resolved from
/// the scoped dependency injection container.
/// </para>
/// <para>
/// <b>Producer pipeline insertion point:</b> after <c>SerializerMiddleware</c>, before <c>TracePropagationMiddleware</c>.<br/>
/// <b>Consumer pipeline insertion point:</b> after <c>DeserializerMiddleware</c>, before <c>HeaderFilterMiddleware</c>.
/// </para>
/// <para>
/// Multiple custom middleware are chained in registration order (FIFO). Each middleware instance
/// has scoped DI lifetime, meaning it is created once per scope and is not shared across scopes.
/// Implementors must set <see cref="IsEnabled"/> to <c>true</c> for the middleware to participate
/// in the pipeline; the default is <c>false</c>.
/// </para>
/// </summary>
/// <typeparam name="T">The message type. Must be a reference type.</typeparam>
public interface IMiddleware<T> where T : class
{
    /// <summary>
    /// Processes the message envelope. This method is called by the pipeline when the middleware
    /// is enabled. Implementations should perform their processing logic (e.g., logging, auditing,
    /// header enrichment) and return the message or <c>null</c> to indicate filtering.
    /// </summary>
    /// <param name="envelope">The message envelope containing the message, headers, payload, and routing key.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The processed message, or <c>null</c> to filter the message.</returns>
    ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled and should participate in the pipeline.
    /// The default is <c>false</c>. Implementations must return <c>true</c> for the middleware to be
    /// included in the processing chain.
    /// </summary>
    bool IsEnabled => false;

    /// <summary>
    /// Gets a value indicating whether this middleware is disabled. This is the logical inverse of <see cref="IsEnabled"/>.
    /// When <c>true</c>, the middleware is skipped during pipeline construction.
    /// </summary>
    bool IsDisabled => !IsEnabled;

    internal IMiddleware<T>? Next { get; set; }
}
