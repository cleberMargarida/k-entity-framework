namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

internal interface IModelCreatingExternal
{
    static abstract Action<ModelBuilder> OnModelCreatingExternal { get; set; }
}
