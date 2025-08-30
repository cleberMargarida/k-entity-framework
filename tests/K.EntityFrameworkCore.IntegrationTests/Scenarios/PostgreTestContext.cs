using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.IntegrationTests.Scenarios;

public sealed class PostgreTestContext(DbContextOptions options) : DbContext(options)
{
    public IEnumerable<IAnnotation> Annotations = [];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Model.AddAnnotations(Annotations);
    }

    public Topic<MessageType> DefaultMessages { get; set; }
    public Topic<MessageTypeB> AlternativeMessages { get; set; }
}
