using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksShared.Messages;
using System.Net;
using System.Text.Json;

namespace SyncWorker;

public class HttpSyncTrigger
{
    private readonly ILogger<HttpSyncTrigger> _logger;
    private readonly IFullSyncOrchestrator _orchestrator;

    public HttpSyncTrigger(ILogger<HttpSyncTrigger> logger, IFullSyncOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;
    }

    [Function(nameof(HttpSyncTrigger))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync/trigger")] HttpRequestData req,
        FunctionContext context)
    {
        var body = await req.ReadAsStringAsync() ?? string.Empty;
        var data = JsonSerializer.Deserialize<FullSyncMessage>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data == null || string.IsNullOrEmpty(data.CompanyId) || string.IsNullOrEmpty(data.UserId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("CompanyId and UserId are required.");
            return bad;
        }

        _logger.LogInformation("HTTP sync trigger: CompanyId={CompanyId} UserId={UserId}", data.CompanyId, data.UserId);

        await _orchestrator.RunAsync(data, context.CancellationToken);

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync("Sync completed.");
        return ok;
    }
}
