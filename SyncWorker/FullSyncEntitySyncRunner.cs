using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker;

/// <summary>
/// Runs <see cref="IFullSyncEntitySyncStep"/> items with retry and QBO sync-state updates (extracted from <see cref="FullSyncOrchestrator"/>).
/// </summary>
public sealed class FullSyncEntitySyncRunner
{
    private const int MaxRetryCount = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly ILogger<FullSyncEntitySyncRunner> _logger;

    public FullSyncEntitySyncRunner(ILogger<FullSyncEntitySyncRunner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunStepsAsync(
        IReadOnlyList<IFullSyncEntitySyncStep> syncSteps,
        IServiceProvider scopedServices,
        int userId,
        string realmId,
        IQboSyncStateRepository qboSyncStateRepo,
        Dictionary<string, int> results,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var step in syncSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RunSingleEntityAsync(
                step.EntityName,
                step.EntityTypeForSyncState,
                userId,
                realmId,
                qboSyncStateRepo,
                () => step.ExecuteAsync(scopedServices, cancellationToken),
                results,
                errors,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunSingleEntityAsync(
        string entityName,
        string entityTypeForSyncState,
        int userId,
        string realmId,
        IQboSyncStateRepository qboSyncStateRepo,
        Func<Task<int>> syncFunc,
        Dictionary<string, int> results,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing {Entity}...", entityName);
        if (userId > 0)
        {
            try
            {
                await qboSyncStateRepo.UpdateStatusAsync(userId, realmId, entityTypeForSyncState, "Running");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set sync state to Running for {Entity}", entityName);
            }
        }

        Exception? lastException = null;
        for (var attempt = 0; attempt <= MaxRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (attempt > 0)
                {
                    _logger.LogInformation("Retrying {Entity} (attempt {Attempt}/{Total})...", entityName, attempt + 1, MaxRetryCount + 1);
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }

                var count = await syncFunc().ConfigureAwait(false);
                results[entityName] = count;

                if (userId > 0)
                {
                    try
                    {
                        await qboSyncStateRepo.UpdateStatusAsync(userId, realmId, entityTypeForSyncState, "Completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set sync state to Completed for {Entity}", entityName);
                    }
                }

                _logger.LogInformation("Synced {Count} {Entity}", count, entityName);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "Failed to sync {Entity} (attempt {Attempt}/{Total})", entityName, attempt + 1, MaxRetryCount + 1);
            }
        }

        if (userId > 0)
        {
            try
            {
                await qboSyncStateRepo.UpdateStatusAsync(userId, realmId, entityTypeForSyncState, "Failed");
            }
            catch (Exception statusEx)
            {
                _logger.LogWarning(statusEx, "Failed to update QBO Sync State to Failed for {Entity}", entityName);
            }
        }

        errors.Add($"{entityName}: {lastException!.Message}");
    }
}
