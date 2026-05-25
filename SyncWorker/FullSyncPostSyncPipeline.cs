using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker;

/// <summary>
/// Warehouse rebuild and anomaly detection after entity sync (in-process stage boundary).
/// </summary>
public sealed class FullSyncPostSyncPipeline
{
    private readonly ILogger<FullSyncPostSyncPipeline> _logger;

    public FullSyncPostSyncPipeline(ILogger<FullSyncPostSyncPipeline> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(IServiceProvider scopedServices, string companyId, string userId, int userIdInt, string realmId, CancellationToken cancellationToken)
    {
        try
        {
            var warehouse = scopedServices.GetRequiredService<IFinancialWarehouseService>();
            await warehouse.RebuildForCompanyAsync(realmId, userId, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Financial warehouse rebuilt for CompanyId={CompanyId}", companyId);

            try
            {
                var anomalyService = scopedServices.GetRequiredService<IAnomalyDetectionService>();
                await anomalyService.DetectAsync(userIdInt, realmId, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Anomaly detection completed for CompanyId={CompanyId}", companyId);
            }
            catch (Exception anomalyEx)
            {
                _logger.LogError(anomalyEx, "Anomaly detection failed for CompanyId={CompanyId}", companyId);
            }
        }
        catch (Exception aggEx)
        {
            _logger.LogError(aggEx, "Failed to rebuild financial warehouse for CompanyId={CompanyId}", companyId);
        }
    }
}
