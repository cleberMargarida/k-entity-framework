namespace K.EntityFrameworkCore.IntegrationTests.Scenarios;

public sealed class SingleTopicDbContext(DbContextOptions options) : DbContext(options), IModelCreatingExternal
{
    public static Action<ModelBuilder> OnModelCreatingExternal { get; set; } = static _ => { };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingExternal(modelBuilder);
    }

    public Topic<MessageType> Messages { get; set; }
}
