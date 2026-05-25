using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksShared.Messages;

namespace SyncWorker;

/// <summary>
/// Top-level sequencing for full-company QBO sync: session bootstrap, status lifecycle, entity steps, post-sync, completion hooks.
/// </summary>
public sealed class FullSyncOrchestrator : IFullSyncOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FullSyncOrchestrator> _logger;
    private readonly IReadOnlyList<IFullSyncEntitySyncStep> _syncSteps;
    private readonly IReadOnlyList<IFullSyncCompletedSubscriber> _completionSubscribers;
    private readonly FullSyncCompanyStatusLifecycle _statusLifecycle;
    private readonly FullSyncEntitySyncRunner _entitySyncRunner;
    private readonly FullSyncPostSyncPipeline _postSyncPipeline;
    private readonly FullSyncCompletionDispatcher _completionDispatcher;

    public FullSyncOrchestrator(
        IServiceScopeFactory scopeFactory,
        ILogger<FullSyncOrchestrator> logger,
        IEnumerable<IFullSyncEntitySyncStep> syncSteps,
        IEnumerable<IFullSyncCompletedSubscriber> completionSubscribers,
        FullSyncCompanyStatusLifecycle statusLifecycle,
        FullSyncEntitySyncRunner entitySyncRunner,
        FullSyncPostSyncPipeline postSyncPipeline,
        FullSyncCompletionDispatcher completionDispatcher)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncSteps = syncSteps?.ToList() ?? throw new ArgumentNullException(nameof(syncSteps));
        _completionSubscribers = completionSubscribers?.ToList() ?? throw new ArgumentNullException(nameof(completionSubscribers));
        _statusLifecycle = statusLifecycle ?? throw new ArgumentNullException(nameof(statusLifecycle));
        _entitySyncRunner = entitySyncRunner ?? throw new ArgumentNullException(nameof(entitySyncRunner));
        _postSyncPipeline = postSyncPipeline ?? throw new ArgumentNullException(nameof(postSyncPipeline));
        _completionDispatcher = completionDispatcher ?? throw new ArgumentNullException(nameof(completionDispatcher));
    }

    public async Task<FullSyncOrchestrationResult> RunAsync(FullSyncMessage data, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var syncContext = sp.GetRequiredService<SyncContext>();
        FullSyncRunBootstrap.InitializeSyncContext(syncContext, data);

        _logger.LogInformation(
            "Full sync starting CorrelationId={CorrelationId} CompanyId={CompanyId}",
            syncContext.CorrelationId,
            data.CompanyId);

        await _statusLifecycle.SetRunningAsync(sp, data.CompanyId, cancellationToken).ConfigureAwait(false);

        var userId = FullSyncRunBootstrap.TryParseUserId(data.UserId, _logger);
        var results = new Dictionary<string, int>();
        var errors = new List<string>();
        var realmId = data.CompanyId;

        var qboSyncStateRepo = sp.GetRequiredService<IQboSyncStateRepository>();
        await _entitySyncRunner.RunStepsAsync(
            _syncSteps,
            sp,
            userId,
            realmId,
            qboSyncStateRepo,
            results,
            errors,
            cancellationToken).ConfigureAwait(false);

        await _postSyncPipeline.RunAsync(sp, data.CompanyId, data.UserId, userId, realmId, cancellationToken).ConfigureAwait(false);

        await _statusLifecycle.SetFinishedAsync(sp, data.CompanyId, errors, results, cancellationToken).ConfigureAwait(false);

        var completionContext = FullSyncRunBootstrap.CreateCompletionContext(
            data,
            syncContext,
            errors.Count == 0,
            results,
            errors);

        await _completionDispatcher.DispatchAsync(_completionSubscribers, completionContext, data.CompanyId, cancellationToken)
            .ConfigureAwait(false);

        return new FullSyncOrchestrationResult
        {
            EntityCounts = results,
            Errors = errors
        };
    }
}
