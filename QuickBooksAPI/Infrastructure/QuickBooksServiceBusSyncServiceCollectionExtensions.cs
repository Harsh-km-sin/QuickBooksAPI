using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure.Queue;
using QuickBooksAPI.Services;
using QuickBooksShared.Options;

namespace QuickBooksAPI.Infrastructure;

public static class QuickBooksServiceBusSyncServiceCollectionExtensions
{
    public static IServiceCollection AddQuickBooksServiceBusAndSync(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceBusOptions = new ServiceBusOptions();
        configuration.GetSection("ServiceBus").Bind(serviceBusOptions);

        if (!string.IsNullOrWhiteSpace(serviceBusOptions.ConnectionString))
        {
            services.AddSingleton(new ServiceBusClient(serviceBusOptions.ConnectionString));
            services.AddSingleton<ServiceBusSender>(sp =>
                sp.GetRequiredService<ServiceBusClient>().CreateSender(serviceBusOptions.QueueName));
            services.AddSingleton<IQueuePublisher, ServiceBusPublisher>();
        }
        else if (!string.IsNullOrWhiteSpace(serviceBusOptions.LocalSyncWorkerUrl))
        {
            var url = serviceBusOptions.LocalSyncWorkerUrl;
            services.AddSingleton<IQueuePublisher>(sp =>
                new LocalHttpQueuePublisher(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    url,
                    sp.GetRequiredService<ILogger<LocalHttpQueuePublisher>>()));
        }
        else
        {
            services.AddSingleton<IQueuePublisher, NoOpQueuePublisher>();
        }

        services.AddScoped<ISyncService, SyncService>();
        return services;
    }
}
