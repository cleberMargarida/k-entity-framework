namespace K.EntityFrameworkCore;

public interface IEnvelope<T>
{
    T Message { get; }
}