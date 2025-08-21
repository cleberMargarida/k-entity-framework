using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Batch
{
    /// <summary>
    /// Marker interface indicating that a middleware is the final node 
    /// in the pipeline, meaning no subsequent middleware will be executed.
    /// </summary>
    internal interface IEndMiddleware
    {
    }
}
