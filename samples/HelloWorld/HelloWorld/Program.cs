using HelloWorld;
using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderContext>(optionsBuilder => optionsBuilder

    // Configure EF Core to use SQL Server
    .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Initial Catalog=Hello World")

    // Enable Kafka extensibility for EF Core (publishing/consuming integration)
    .UseKafkaExtensibility(client => client.BootstrapServers = "localhost:9092"));

using var app = builder.Build();

app.Start();

var scope = app.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

// here you're intending to mark the entity to be persisted.
dbContext.Orders.Add(new Order { Status = "New" });

// here you're signing the event to be published.
// not a block calling, the event will be published when SaveChangesAsync is called.
dbContext.OrderEvents.Produce(new OrderEvent { Id = 1, Status = Guid.NewGuid().ToString() });

await dbContext.SaveChangesAsync();

// here you're starting to consume kafka and moving the iterator cursor to the next offset in the assigned partitions.
await foreach (var order in dbContext.OrderEvents.WithCancellation(app.Lifetime.ApplicationStopping))
{
    // here you're commiting the offset of the current event.
    await dbContext.SaveChangesAsync();
}

app.WaitForShutdown();

namespace HelloWorld
{
    public class OrderContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; }

        public Topic<OrderEvent> OrderEvents { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }

    public class OrderEvent
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }
}
