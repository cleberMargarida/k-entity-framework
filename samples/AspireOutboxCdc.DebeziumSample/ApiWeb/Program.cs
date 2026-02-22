using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDb>(options => options
    // configure postgres
    .UseNpgsql(builder.Configuration.GetConnectionString("postgres"))
    // configure kafka
    .UseKafkaExtensibility(builder.Configuration.GetConnectionString("kafka")!));

builder.Services.AddTopicProvisioning<OrderDb>();

var app = builder.Build();

app.Start();

using var scope = app.Services.CreateScope();

var dbcontext = scope.ServiceProvider.GetRequiredService<OrderDb>();

dbcontext.Database.EnsureCreated();

using var transaction = dbcontext.Database.BeginTransaction();

try
{
    var entity = await dbcontext.Orders.AddAsync(new Order
    {
        Id = Guid.CreateVersion7(DateTimeOffset.UtcNow),
        Description = "Sample order"
    });

    dbcontext.OrderEvents.Produce(new OrderPlaced
    {
        Id = entity.Entity.Id,
        PlacedAt = DateTime.UtcNow
    });

    await dbcontext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception)
{
    await transaction.RollbackAsync();
}

await foreach (var orderPlaced in dbcontext.OrderEvents.WithCancellation(app.Lifetime.ApplicationStopping))
{
    Console.WriteLine($"Order placed: {orderPlaced.Id} at {orderPlaced.PlacedAt}");
    break;
}

app.WaitForShutdown();



public class OrderDb(DbContextOptions<OrderDb> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Topic<OrderPlaced>(topic => 
        {
            topic.HasProducer(producer =>
            {
                producer.HasHeader(x => x.PlacedAt);
                producer.HasOutbox();
            });
        });
    }

    public DbSet<Order> Orders { get; set; }
    public Topic<OrderPlaced> OrderEvents { get; set; }
}

public class OrderPlaced
{
    public Guid Id { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public string Description { get; set; }
}