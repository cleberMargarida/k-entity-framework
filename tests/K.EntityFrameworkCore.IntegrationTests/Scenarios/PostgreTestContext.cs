using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios;

public sealed class PostgreTestContext(DbContextOptions options) : DbContext(options), IDisposable, IAsyncDisposable
{
    public static List<IAnnotation> Annotations = [];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasOutboxInboxTables();
        modelBuilder.Model.AddAnnotations(Annotations);
    }

    public Topic<DefaultMessage> DefaultMessages { get; set; }
    public Topic<AlternativeMessage> AlternativeMessages { get; set; }

    public override void Dispose()
    {
        Annotations.Clear();
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        Annotations.Clear();
        return base.DisposeAsync();
    }
}
