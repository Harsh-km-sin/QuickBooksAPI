using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncChartOfAccountsStep : IFullSyncEntitySyncStep
{
    public string EntityName => "ChartOfAccounts";

    public string EntityTypeForSyncState => "Chart_Of_Accounts";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IChartOfAccountsService>();
        var result = await svc.syncChartOfAccounts().ConfigureAwait(false);
        return result.Data;
    }
}
