using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios;

public sealed class DebeziumTestContext(DbContextOptions<DebeziumTestContext> options) : DbContext(options), IDisposable
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static List<IAnnotation> Annotations = [];
#pragma warning restore CA2211

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        modelBuilder.HasOutboxInboxTables();
#pragma warning restore CS0618
        modelBuilder.Model.AddAnnotations(Annotations);
    }

    public DbSet<DebeziumOrder> Orders { get; set; } = null!;
    public Topic<DebeziumOrderPlaced> OrderEvents { get; set; } = null!;

    public override void Dispose()
    {
        Annotations.Clear();
        base.Dispose();
    }
}

public class DebeziumOrder
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
}

public class DebeziumOrderPlaced
{
    public Guid Id { get; set; }
    public DateTime PlacedAt { get; set; }
}
