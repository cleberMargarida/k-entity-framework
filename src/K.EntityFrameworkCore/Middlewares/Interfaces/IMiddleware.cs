namespace K.EntityFrameworkCore.Middlewares.Interfaces;

public interface IMiddleware<T> where T : class
{
    ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default);
}
