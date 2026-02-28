using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios;

public sealed class PostgreTestContext(DbContextOptions options) : DbContext(options), IDisposable, IAsyncDisposable
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static List<IAnnotation> Annotations = [];
#pragma warning restore CA2211 // Non-constant fields should not be visible

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        modelBuilder.HasOutboxInboxTables();
#pragma warning restore CS0618 // Type or member is obsolete
        modelBuilder.Model.AddAnnotations(Annotations);
    }

    public Topic<DefaultMessage> DefaultMessages { get; set; }
    public Topic<AlternativeMessage> AlternativeMessages { get; set; }

    public override void Dispose()
    {
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        return base.DisposeAsync();
    }
}
