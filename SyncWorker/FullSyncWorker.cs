using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using System.Text.Json;

namespace SyncWorker
{
    public class FullSyncWorker
    {
        private const int MaxRetryCount = 2;       // 1 initial + 2 retries = 3 attempts total
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private readonly ILogger<FullSyncWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FullSyncWorker(ILogger<FullSyncWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [Function(nameof(FullSyncWorker))]
        public async Task Run(
            [ServiceBusTrigger("qbo-full-sync", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            FullSyncMessage? data = null;

            try
            {
                var body = message.Body.ToString();
                _logger.LogInformation("Received sync message: {Body}", body);

                data = JsonSerializer.Deserialize<FullSyncMessage>(body);
                if (data == null || string.IsNullOrEmpty(data.CompanyId) || string.IsNullOrEmpty(data.UserId))
                    throw new InvalidOperationException("Invalid sync message: missing CompanyId or UserId.");

                using var scope = _serviceProvider.CreateScope();

                var syncUser = scope.ServiceProvider.GetRequiredService<SyncCurrentUser>();
                syncUser.UserId = data.UserId;
                syncUser.RealmId = data.CompanyId;

                var statusRepo = scope.ServiceProvider.GetRequiredService<ISyncStatusRepository>();
                await statusRepo.SetStatusAsync(data.CompanyId, "Running");

                var qboSyncStateRepo = scope.ServiceProvider.GetRequiredService<IQboSyncStateRepository>();
                if (!int.TryParse(data.UserId, out var userId))
                {
                    _logger.LogWarning("Full sync: UserId could not be parsed as int ({UserId}). QBO Sync State will not be updated.", data.UserId);
                }

                var results = new Dictionary<string, int>();
                var errors = new List<string>();
                var realmId = data.CompanyId;

                await SyncEntity("Customers", "Customer", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<ICustomerService>();
                    var result = await svc.GetCustomersAsync();
                    return result.Data;
                }, results, errors);

                await SyncEntity("Vendors", "Vendors", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IVendorService>();
                    var result = await svc.GetVendorsAsync();
                    return result.Data;
                }, results, errors);

                await SyncEntity("Products", "Products", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IProductService>();
                    var result = await svc.GetProductsAsync();
                    return result.Data;
                }, results, errors);

                await SyncEntity("ChartOfAccounts", "Chart_Of_Accounts", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IChartOfAccountsService>();
                    var result = await svc.syncChartOfAccounts();
                    return result.Data;
                }, results, errors);

                await SyncEntity("Invoices", "Invoice", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                    var result = await svc.SyncInvoicesAsync();
                    return result.Data;
                }, results, errors);

                await SyncEntity("Bills", "Bills", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IBillService>();
                    var result = await svc.SyncBillsAsync();
                    return result.Data;
                }, results, errors);

                await SyncEntity("JournalEntries", "Manual_Journals", userId, realmId, qboSyncStateRepo, async () =>
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IJournalEntryService>();
                    var result = await svc.SyncJournalEntriesAsync();
                    return result.Data;
                }, results, errors);

                if (errors.Count > 0)
                {
                    var errorSummary = string.Join("; ", errors);
                    await statusRepo.SetStatusAsync(data.CompanyId, "PartiallyFailed", errorSummary);
                    _logger.LogWarning("Full sync partially failed for {CompanyId}: {Errors}", data.CompanyId, errorSummary);
                }
                else
                {
                    await statusRepo.SetStatusAsync(data.CompanyId, "Completed");
                    _logger.LogInformation("Full sync completed for {CompanyId}. Results: {@Results}", data.CompanyId, results);
                }

                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full sync failed for CompanyId={CompanyId}, MessageId={MessageId}",
                    data?.CompanyId ?? "unknown", message.MessageId);

                if (data != null)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var statusRepo = scope.ServiceProvider.GetRequiredService<ISyncStatusRepository>();
                        await statusRepo.SetStatusAsync(data.CompanyId, "Failed", ex.Message);
                    }
                    catch (Exception statusEx)
                    {
                        _logger.LogError(statusEx, "Failed to update sync status for {CompanyId}", data.CompanyId);
                    }
                }

                throw;
            }
        }

        private async Task SyncEntity(string entityName, string entityTypeForSyncState,
            int userId, string realmId, IQboSyncStateRepository qboSyncStateRepo,
            Func<Task<int>> syncFunc, Dictionary<string, int> results, List<string> errors)
        {
            _logger.LogInformation("Syncing {Entity}...", entityName);
            if (userId > 0)
            {
                try
                {
                    await qboSyncStateRepo.UpdateStatusAsync(userId, realmId, entityTypeForSyncState, "Running");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set sync state to Running for {Entity}", entityName);
                }
            }

            Exception? lastException = null;
            for (var attempt = 0; attempt <= MaxRetryCount; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retrying {Entity} (attempt {Attempt}/{Total})...", entityName, attempt + 1, MaxRetryCount + 1);
                        await Task.Delay(RetryDelay);
                    }

                    var count = await syncFunc();
                    results[entityName] = count;

                    if (userId > 0)
                    {
                        try
                        {
                            await qboSyncStateRepo.UpdateStatusAsync(userId, realmId, entityTypeForSyncState, "Completed");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to set sync state to Completed for {Entity}", entityName);
                        }
                    }
                    _logger.LogInformation("Synced {Count} {Entity}", count, entityName);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Failed to sync {Entity} (attempt {Attempt}/{Total})", entityName, attempt + 1, MaxRetryCount + 1);
                }
            }

            if (userId > 0)
            {
                try
                {
                    await qboSyncStateRepo.UpdateStatusAsync(userId, realmId, entityTypeForSyncState, "Failed");
                }
                catch (Exception statusEx)
                {
                    _logger.LogWarning(statusEx, "Failed to update QBO Sync State to Failed for {Entity}", entityName);
                }
            }
            errors.Add($"{entityName}: {lastException!.Message}");
        }
    }
}
