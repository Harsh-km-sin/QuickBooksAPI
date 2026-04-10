using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncInvoicesStep : IFullSyncEntitySyncStep
{
    public string EntityName => "Invoices";

    public string EntityTypeForSyncState => "Invoice";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IInvoiceService>();
        var result = await svc.SyncInvoicesAsync().ConfigureAwait(false);
        return result.Data;
    }
}
