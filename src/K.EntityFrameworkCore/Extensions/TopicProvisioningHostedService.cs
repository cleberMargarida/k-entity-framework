using Confluent.Kafka;
using Confluent.Kafka.Admin;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// A hosted service that automatically provisions Kafka topics based on TopicSpecification configurations
/// registered in the DbContext model using the HasSetting method.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type to scan for topic configurations.</typeparam>
/// <remarks>
/// <para>
/// This service runs during application startup and scans the specified DbContext for topic configurations
/// that have been registered using the fluent API with <c>HasSetting</c>. It then connects to Kafka
/// using the Admin API to check which topics already exist and creates any missing topics.
/// </para>
/// <para>
/// The service will:
/// <list type="bullet">
/// <item><description>Scan the DbContext model for all TopicSpecification configurations</description></item>
/// <item><description>Connect to Kafka using the Admin API</description></item>
/// <item><description>Check which topics already exist to avoid conflicts</description></item>
/// <item><description>Create only the topics that don't exist</description></item>
/// <item><description>Log all operations for monitoring and debugging</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong> This service requires Kafka to be accessible during application startup.
/// If Kafka is not available, the service will fail and prevent the application from starting.
/// </para>
/// <example>
/// Register the service using the extension method:
/// <code>
/// services.AddTopicProvisioning&lt;MyDbContext&gt;();
/// </code>
/// </example>
/// <seealso cref="ServiceCollectionExtensions.AddTopicProvisioning{TDbContext}(IServiceCollection)"/>
/// </remarks>
internal class TopicProvisioningHostedService<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<TopicProvisioningHostedService<TDbContext>> logger) : IHostedService
    where TDbContext : DbContext
{
    /// <summary>
    /// Starts the service and provisions all configured topics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item><description>Creates a service scope to resolve the DbContext</description></item>
    /// <item><description>Scans the DbContext model for TopicSpecification configurations</description></item>
    /// <item><description>Sets up the Kafka Admin client with connection configuration</description></item>
    /// <item><description>Retrieves existing topics from Kafka to avoid duplicates</description></item>
    /// <item><description>Creates only the topics that don't already exist</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If any topic creation fails (except for "already exists" errors), this method will throw an exception
    /// which will prevent the application from starting. This ensures that topic provisioning issues are
    /// caught early in the application lifecycle.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown when topic provisioning fails due to Kafka connection issues or topic creation errors.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting topic provisioning for DbContext {DbContextType}", typeof(TDbContext).Name);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            IServiceProvider infrastructure = dbContext.GetInfrastructure();
            var clientConfig = infrastructure.GetRequiredService<ClientConfig>();

            var topicSpecifications = dbContext.Model.GetAllTopicSpecifications();

            if (topicSpecifications.Count == 0)
            {
                logger.LogInformation("No topic specifications found in DbContext {DbContextType}", typeof(TDbContext).Name);
                return;
            }

            logger.LogInformation("Found {Count} topic specifications to provision", topicSpecifications.Count);

            using var adminClient = new AdminClientBuilder(clientConfig).Build();

            var topicsToCreate = new List<TopicSpecification>();

            foreach (var (messageType, specification) in topicSpecifications)
            {
                var clientSettings = (IClientSettings)infrastructure.GetService(typeof(ClientSettings<>).MakeGenericType(messageType))!;

                if (string.IsNullOrEmpty(specification.Name))
                {
                    specification.Name = clientSettings.TopicName;
                }

                topicsToCreate.Add(specification);
                logger.LogDebug("Prepared topic specification for {MessageType}: {TopicName}", messageType.Name, specification.Name);
            }

            await CreateTopicsAsync(adminClient, topicsToCreate);

            logger.LogInformation("Topic provisioning completed successfully for DbContext {DbContextType}",
                typeof(TDbContext).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision topics for DbContext {DbContextType}", typeof(TDbContext).Name);
            throw;
        }
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping topic provisioning service for DbContext {DbContextType}", typeof(TDbContext).Name);
        return Task.CompletedTask;
    }

    private async Task CreateTopicsAsync(IAdminClient adminClient, List<TopicSpecification> topicsToCreate)
    {
        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            var newTopics = topicsToCreate.Where(t => !existingTopics.Contains(t.Name)).ToList();

            if (newTopics.Count == 0)
            {
                logger.LogInformation("All topics already exist, no new topics to create");
                return;
            }

            logger.LogInformation("Creating {Count} new topics", newTopics.Count);

            await adminClient.CreateTopicsAsync(newTopics, new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(30),
                OperationTimeout = TimeSpan.FromSeconds(30)
            });

            foreach (var topic in newTopics)
            {
                logger.LogInformation("Successfully created topic: {TopicName} with {Partitions} partitions and replication factor {ReplicationFactor}",
                    topic.Name, topic.NumPartitions, topic.ReplicationFactor);
            }
        }
        catch (CreateTopicsException ex)
        {
            foreach (var result in ex.Results)
            {
                if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                {
                    logger.LogInformation("Topic {TopicName} already exists", result.Topic);
                }
                else if (result.Error.Code != ErrorCode.NoError)
                {
                    logger.LogError("Failed to create topic {TopicName}: {Error}", result.Topic, result.Error.Reason);
                }
                else
                {
                    logger.LogInformation("Successfully created topic: {TopicName}", result.Topic);
                }
            }

            var hasRealErrors = ex.Results.Any(r => r.Error.Code is not ErrorCode.NoError and not ErrorCode.TopicAlreadyExists);
            if (hasRealErrors)
            {
                throw;
            }
        }
    }
}