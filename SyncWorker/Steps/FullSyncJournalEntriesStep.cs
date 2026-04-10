using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;

namespace SyncWorker.Steps;

internal sealed class FullSyncJournalEntriesStep : IFullSyncEntitySyncStep
{
    public string EntityName => "JournalEntries";

    public string EntityTypeForSyncState => "Manual_Journals";

    public async Task<int> ExecuteAsync(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var svc = scopedServices.GetRequiredService<IJournalEntryService>();
        var result = await svc.SyncJournalEntriesAsync().ConfigureAwait(false);
        return result.Data;
    }
}
