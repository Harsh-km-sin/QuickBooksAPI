using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncCustomersStep : IFullSyncEntitySyncStep
{
    public string EntityName => "Customers";

    public string EntityTypeForSyncState => "Customer";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<ICustomerService>();
        var result = await svc.GetCustomersAsync().ConfigureAwait(false);
        return result.Data;
    }
}
