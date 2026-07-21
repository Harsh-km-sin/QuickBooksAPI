using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

/// <summary>
/// Pulls P&amp;L and Balance Sheet history as part of a full sync.
///
/// Registered after <see cref="FullSyncChartOfAccountsStep"/> because report rows carry account
/// references that are only meaningful once accounts are resident.
///
/// Uses the non-forcing path, so the pull is skipped when no financial entity changed in the steps
/// that ran before it. The manual "Generate Reports" endpoint forces instead.
/// </summary>
internal sealed class FullSyncReportsStep : IFullSyncEntitySyncStep
{
    public string EntityName => "Reports";

    public string EntityTypeForSyncState => "Reports";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IReportService>();
        var result = await svc.SyncReportsForFullSyncAsync().ConfigureAwait(false);
        return result.Data;
    }
}
