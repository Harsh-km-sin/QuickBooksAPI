using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.DataAccessLayer.Sql;
using QuickBooksAPI.Infrastructure.BlobStorage;
using QuickBooksAPI.Infrastructure.Queue;

namespace QuickBooksAPI.Infrastructure;

public static class GlReviewServiceCollectionExtensions
{
    public static IServiceCollection AddGlReview(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IGlRunRepository>(sp =>
            new GlRunRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IGlTransactionRepository>(sp =>
            new GlTransactionRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IGlSettingsRepository>(sp =>
            new GlSettingsRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IGlAnomalyRepository>(sp =>
            new GlAnomalyRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IGlFeedbackRepository>(sp =>
            new GlFeedbackRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IGlBusinessRuleRepository>(sp =>
            new GlBusinessRuleRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IGlChatRepository>(sp =>
            new GlChatRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        // ── Blob storage ──────────────────────────────────────────────────────
        var blobConnString = configuration["BlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobConnString))
        {
            services.AddSingleton(new BlobServiceClient(blobConnString));
            services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        }
        else
        {
            services.AddScoped<IBlobStorageService, LocalFileBlobStorageService>();
        }

        // ── GL analysis queue (separate from QBO sync queue) ──────────────────
        var glQueueName = configuration["ServiceBus:GlAnalysisQueueName"];
        var sbConnString = configuration["ServiceBus:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(sbConnString) && !string.IsNullOrWhiteSpace(glQueueName))
        {
            services.AddSingleton<IGlQueuePublisher>(sp =>
            {
                var client = sp.GetRequiredService<ServiceBusClient>();
                var sender = client.CreateSender(glQueueName);
                return new GlServiceBusPublisher(sender);
            });
        }
        else
        {
            services.AddSingleton<IGlQueuePublisher, NoOpGlQueuePublisher>();
        }

        return services;
    }
}
