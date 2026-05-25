using Microsoft.Extensions.DependencyInjection;

namespace SyncWorker;

/// <summary>
/// Worker-side sync pipeline (orchestrator dependencies and related singletons).
/// </summary>
public static class WorkerSyncPipelineServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerSyncPipeline(this IServiceCollection services)
    {
        services.AddSingleton<FullSyncEntitySyncRunner>();
        services.AddSingleton<FullSyncPostSyncPipeline>();
        services.AddSingleton<FullSyncCompanyStatusLifecycle>();
        services.AddSingleton<FullSyncCompletionDispatcher>();
        return services;
    }
}
