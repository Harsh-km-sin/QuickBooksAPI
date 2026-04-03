using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using System.Text.Json;

namespace SyncWorker;

public class FullSyncWorker
{
    private readonly ILogger<FullSyncWorker> _logger;
    private readonly IFullSyncOrchestrator _orchestrator;
    private readonly IServiceScopeFactory _scopeFactory;

    public FullSyncWorker(
        ILogger<FullSyncWorker> logger,
        IFullSyncOrchestrator orchestrator,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    [Function(nameof(FullSyncWorker))]
    public async Task Run(
        [ServiceBusTrigger("qbo-full-sync", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        FunctionContext context)
    {
        FullSyncMessage? data = null;

        try
        {
            var body = message.Body.ToString();
            _logger.LogInformation("Received sync message: {Body}", body);

            data = JsonSerializer.Deserialize<FullSyncMessage>(body);
            if (data == null || string.IsNullOrEmpty(data.CompanyId) || string.IsNullOrEmpty(data.UserId))
                throw new InvalidOperationException("Invalid sync message: missing CompanyId or UserId.");

            var ct = context.CancellationToken;
            await _orchestrator.RunAsync(data, ct);

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
                    using var scope = _scopeFactory.CreateScope();
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
}
