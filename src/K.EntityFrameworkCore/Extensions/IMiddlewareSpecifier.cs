
namespace K.EntityFrameworkCore.Extensions
{
    public interface IMiddlewareSpecifier<T>
    {
        ScopedCommand? DeferedExecution(OutboxMessage outboxMessage);
    }
}