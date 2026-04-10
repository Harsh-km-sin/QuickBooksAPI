using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncProductsStep : IFullSyncEntitySyncStep
{
    public string EntityName => "Products";

    public string EntityTypeForSyncState => "Products";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IProductService>();
        var result = await svc.GetProductsAsync().ConfigureAwait(false);
        return result.Data;
    }
}
