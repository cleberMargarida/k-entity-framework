namespace K.EntityFrameworkCore;

public interface IEnvelope<out T>
{
    T Message { get; }
}