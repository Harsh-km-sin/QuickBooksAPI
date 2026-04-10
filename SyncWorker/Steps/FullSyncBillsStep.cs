using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncBillsStep : IFullSyncEntitySyncStep
{
    public string EntityName => "Bills";

    public string EntityTypeForSyncState => "Bills";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IBillService>();
        var result = await svc.SyncBillsAsync().ConfigureAwait(false);
        return result.Data;
    }
}
