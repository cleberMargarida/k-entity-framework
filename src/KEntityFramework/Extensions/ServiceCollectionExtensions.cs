using KEntityFramework;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrokerContext<T>(
        this IServiceCollection services,
        string connectionString)
        where T : BrokerContext => services.AddSingleton(serviceProvider =>
        {
            T instance = ActivatorUtilities.CreateInstance<T>(
                serviceProvider,
                connectionString);

            return instance;
        });
}
