using Microsoft.Extensions.DependencyInjection;

namespace QuickBooksAPI.Infrastructure;

/// <summary>
/// Application-layer registrations shared by the API host and SyncWorker (auth, entity façades). QuickBooks Online HTTP adapters are registered via <c>AddQuickBooksOnlineIntegration</c> in <c>QuickBooksAPI.Integrations.QuickBooks</c>.
/// </summary>
public static partial class QuickBooksApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddQuickBooksAuthAndEntityApplicationServices(this IServiceCollection services)
    {
        services.AddQuickBooksAuthAndIntegrationApplicationServices();
        services.AddQuickBooksEntityApplicationServices();
        return services;
    }
}
