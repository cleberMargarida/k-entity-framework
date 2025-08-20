namespace K.EntityFrameworkCore.Middlewares.Forget;

/// <summary>
/// Defines the execution strategy for forget middleware.
/// </summary>
public enum ForgetStrategy
{
    /// <summary>
    /// Awaits the processing (with optional timeout) but ignores exceptions.
    /// The caller waits for completion or timeout before continuing.
    /// </summary>
    AwaitForget,

    /// <summary>
    /// Starts the processing asynchronously and immediately returns.
    /// The caller does not wait for completion (true fire-and-forget).
    /// </summary>
    FireForget
}
