using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        // configure db context with connection strings from configuration
        services.AddDbContext<OrderDb>(options => options
            .UseNpgsql(context.Configuration.GetConnectionString("postgres"))
            .UseKafkaExtensibility(context.Configuration.GetConnectionString("kafka")!));

        services.AddTopicProvisioning<OrderDb>();
    })
    .Build();

await host.StartAsync();

using (var scope = host.Services.CreateScope())
{
    var dbcontext = scope.ServiceProvider.GetRequiredService<OrderDb>();

    dbcontext.Database.EnsureCreated();

    using var transaction = await dbcontext.Database.BeginTransactionAsync();
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

    await foreach (var orderPlaced in dbcontext.OrderEvents)
    {
        Console.WriteLine($"Order placed: {orderPlaced.Id} at {orderPlaced.PlacedAt}");
        break;
    }
}

await host.StopAsync();


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

    public DbSet<Order> Orders { get; set; } = null!;
    public Topic<OrderPlaced> OrderEvents { get; set; } = null!;
}

public class OrderPlaced
{
    public Guid Id { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public string Description { get; set; } = null!;
}