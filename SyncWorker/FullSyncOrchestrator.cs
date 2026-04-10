using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksShared.Messages;

namespace SyncWorker;

/// <summary>
/// Coordinates full-company QBO sync, warehouse rebuild, and anomaly detection. Entity steps are <see cref="IFullSyncEntitySyncStep"/> implementations registered in order.
/// </summary>
public sealed class FullSyncOrchestrator : IFullSyncOrchestrator
{
    private const int MaxRetryCount = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FullSyncOrchestrator> _logger;
    private readonly IReadOnlyList<IFullSyncEntitySyncStep> _syncSteps;
    private readonly IReadOnlyList<IFullSyncCompletedSubscriber> _completionSubscribers;

    public FullSyncOrchestrator(
        IServiceScopeFactory scopeFactory,
        ILogger<FullSyncOrchestrator> logger,
        IEnumerable<IFullSyncEntitySyncStep> syncSteps,
        IEnumerable<IFullSyncCompletedSubscriber> completionSubscribers)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncSteps = syncSteps?.ToList() ?? throw new ArgumentNullException(nameof(syncSteps));
        _completionSubscribers = completionSubscribers?.ToList() ?? throw new ArgumentNullException(nameof(completionSubscribers));
    }

    public async Task<FullSyncOrchestrationResult> RunAsync(FullSyncMessage data, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var syncContext = sp.GetRequiredService<SyncContext>();
        syncContext.UserId = data.UserId;
        syncContext.RealmId = data.CompanyId;
        syncContext.CorrelationId = string.IsNullOrWhiteSpace(data.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : data.CorrelationId.Trim();

        _logger.LogInformation(
            "Full sync starting CorrelationId={CorrelationId} CompanyId={CompanyId}",
            syncContext.CorrelationId,
            data.CompanyId);

        var statusRepo = sp.GetRequiredService<ISyncStatusRepository>();
        await statusRepo.SetStatusAsync(data.CompanyId, "Running");

        var qboSyncStateRepo = sp.GetRequiredService<IQboSyncStateRepository>();
        if (!int.TryParse(data.UserId, out var userId))
        {
            _logger.LogWarning("Full sync: UserId could not be parsed as int ({UserId}). QBO Sync State will not be updated.", data.UserId);
        }

        var results = new Dictionary<string, int>();
        var errors = new List<string>();
        var realmId = data.CompanyId;

        foreach (var step in _syncSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SyncEntityAsync(step.EntityName, step.EntityTypeForSyncState, userId, realmId, qboSyncStateRepo,
                () => step.ExecuteAsync(sp, cancellationToken), results, errors, cancellationToken);
        }

        try
        {
            var warehouse = sp.GetRequiredService<IFinancialWarehouseService>();
            await warehouse.RebuildForCompanyAsync(realmId, data.UserId, cancellationToken);
            _logger.LogInformation("Financial warehouse rebuilt for CompanyId={CompanyId}", data.CompanyId);

            try
            {
                var anomalyService = sp.GetRequiredService<IAnomalyDetectionService>();
                await anomalyService.DetectAsync(userId, realmId, cancellationToken);
                _logger.LogInformation("Anomaly detection completed for CompanyId={CompanyId}", data.CompanyId);
            }
            catch (Exception anomalyEx)
            {
                _logger.LogError(anomalyEx, "Anomaly detection failed for CompanyId={CompanyId}", data.CompanyId);
            }
        }
        catch (Exception aggEx)
        {
            _logger.LogError(aggEx, "Failed to rebuild financial warehouse for CompanyId={CompanyId}", data.CompanyId);
        }

        if (errors.Count > 0)
        {
            var errorSummary = string.Join("; ", errors);
            await statusRepo.SetStatusAsync(data.CompanyId, "PartiallyFailed", errorSummary);
            _logger.LogWarning("Full sync partially failed for {CompanyId}: {Errors}", data.CompanyId, errorSummary);
        }
        else
        {
            await statusRepo.SetStatusAsync(data.CompanyId, "Completed");
            _logger.LogInformation("Full sync completed for {CompanyId}. Results: {@Results}", data.CompanyId, results);
        }

        var completionContext = new FullSyncCompletionContext(
            data.CompanyId,
            data.UserId,
            syncContext.CorrelationId ?? string.Empty,
            errors.Count == 0,
            results,
            errors);

        foreach (var subscriber in _completionSubscribers)
        {
            try
            {
                await subscriber.OnFullSyncCompletedAsync(completionContext, cancellationToken);
            }
            catch (Exception subEx)
            {
                _logger.LogWarning(subEx, "IFullSyncCompletedSubscriber failed for CompanyId={CompanyId}", data.CompanyId);
            }
        }

        return new FullSyncOrchestrationResult
        {
            EntityCounts = results,
            Errors = errors
        };
    }

    private async Task SyncEntityAsync(
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
                    await Task.Delay(RetryDelay, cancellationToken);
                }

                var count = await syncFunc();
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
