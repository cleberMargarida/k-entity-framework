namespace K.EntityFrameworkCore.Interfaces;

/// <summary>
/// Defines a configuration interface for middleware that can be applied to a message processing pipeline.
/// </summary>
public interface IMiddlewareConfig<T>
    where T : class
{
    /// <summary>
    /// Registers a middleware of type <typeparamref name="T"/> to be used in the processing pipeline.
    /// </summary>
    void Use(IMiddleware<T> middleware);
}
