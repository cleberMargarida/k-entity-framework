using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add Kafka-related hosted services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a hosted service that automatically provisions Kafka topics based on TopicSpecification configurations
    /// registered in the DbContext model using the HasSetting method.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the Entity Framework Core <see cref="DbContext"/> to scan for topic configurations.</typeparam>
    /// <param name="services">The service collection to add the hosted service to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This hosted service will start during application startup and automatically create any Kafka topics 
    /// that have been configured with <c>HasSetting</c> in the DbContext's <c>OnModelCreating</c> method 
    /// but don't yet exist in the Kafka cluster.
    /// </para>
    /// <para>
    /// Topics that already exist will be skipped to avoid conflicts. The service uses the Kafka Admin API
    /// to check for existing topics and only creates new ones.
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong>
    /// <list type="bullet">
    /// <item><description>The DbContext must be registered in the DI container</description></item>
    /// <item><description>Kafka cluster must be accessible during application startup</description></item>
    /// <item><description>Topics must be configured using the fluent API in OnModelCreating</description></item>
    /// </list>
    /// </para>
    /// see <seealso cref="TopicProvisioningHostedService{TDbContext}"/>
    /// </remarks>
    public static IServiceCollection AddTopicProvisioning<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddHostedService<TopicProvisioningHostedService<TDbContext>>();
        return services;
    }
}
