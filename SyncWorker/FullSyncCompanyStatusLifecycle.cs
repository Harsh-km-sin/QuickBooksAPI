using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker;

/// <summary>
/// High-level company sync status transitions (Running → Completed / PartiallyFailed) for the worker dashboard.
/// </summary>
public sealed class FullSyncCompanyStatusLifecycle
{
    private readonly ILogger<FullSyncCompanyStatusLifecycle> _logger;

    public FullSyncCompanyStatusLifecycle(ILogger<FullSyncCompanyStatusLifecycle> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SetRunningAsync(IServiceProvider scopedServices, string companyId, CancellationToken cancellationToken = default)
    {
        var statusRepo = scopedServices.GetRequiredService<ISyncStatusRepository>();
        await statusRepo.SetStatusAsync(companyId, "Running").ConfigureAwait(false);
    }

    public async Task SetFinishedAsync(
        IServiceProvider scopedServices,
        string companyId,
        IReadOnlyList<string> errors,
        IReadOnlyDictionary<string, int> entityCounts,
        CancellationToken cancellationToken = default)
    {
        var statusRepo = scopedServices.GetRequiredService<ISyncStatusRepository>();
        if (errors.Count > 0)
        {
            var errorSummary = string.Join("; ", errors);
            await statusRepo.SetStatusAsync(companyId, "PartiallyFailed", errorSummary).ConfigureAwait(false);
            _logger.LogWarning("Full sync partially failed for {CompanyId}: {Errors}", companyId, errorSummary);
        }
        else
        {
            await statusRepo.SetStatusAsync(companyId, "Completed").ConfigureAwait(false);
            _logger.LogInformation("Full sync completed for {CompanyId}. Results: {@Results}", companyId, entityCounts);
        }
    }
}
