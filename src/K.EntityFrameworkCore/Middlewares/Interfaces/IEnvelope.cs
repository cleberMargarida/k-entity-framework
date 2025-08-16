namespace K.EntityFrameworkCore.Middlewares.Interfaces;

public interface IEnvelope<out T>
{
    T Message { get; }
}