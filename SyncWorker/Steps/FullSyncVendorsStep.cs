using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncVendorsStep : IFullSyncEntitySyncStep
{
    public string EntityName => "Vendors";

    public string EntityTypeForSyncState => "Vendors";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IVendorService>();
        var result = await svc.GetVendorsAsync().ConfigureAwait(false);
        return result.Data;
    }
}
