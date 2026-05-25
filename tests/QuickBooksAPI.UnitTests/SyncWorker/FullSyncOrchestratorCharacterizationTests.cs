using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksShared.Messages;
using SyncWorker;

namespace QuickBooksAPI.UnitTests.SyncWorker;

/// <summary>
/// Locks orchestration sequencing (status, steps, warehouse, completion) before further extraction.
/// </summary>
public sealed class FullSyncOrchestratorCharacterizationTests
{
    [Fact]
    public async Task RunAsync_AllStepsSucceed_SetsCompletedAndInvokesSubscribers()
    {
        var statusRepo = new Mock<ISyncStatusRepository>();
        statusRepo.Setup(s => s.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var qboSync = new Mock<IQboSyncStateRepository>();
        qboSync.Setup(q => q.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var warehouse = new Mock<IFinancialWarehouseService>();
        warehouse.Setup(w => w.RebuildForCompanyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var anomaly = new Mock<IAnomalyDetectionService>();
        anomaly.Setup(a => a.DetectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(statusRepo.Object);
        services.AddSingleton(qboSync.Object);
        services.AddSingleton(warehouse.Object);
        services.AddSingleton(anomaly.Object);
        services.AddScoped<SyncContext>();

        await using var provider = services.BuildServiceProvider();

        var step = new Mock<IFullSyncEntitySyncStep>();
        step.Setup(s => s.EntityName).Returns("TestEntity");
        step.Setup(s => s.EntityTypeForSyncState).Returns("TestEntity");
        step.Setup(s => s.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var subscriber = new Mock<IFullSyncCompletedSubscriber>();
        subscriber.Setup(s => s.OnFullSyncCompletedAsync(It.IsAny<FullSyncCompletionContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var orchestrator = new FullSyncOrchestrator(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<FullSyncOrchestrator>.Instance,
            new[] { step.Object },
            new[] { subscriber.Object },
            new FullSyncCompanyStatusLifecycle(NullLogger<FullSyncCompanyStatusLifecycle>.Instance),
            new FullSyncEntitySyncRunner(NullLogger<FullSyncEntitySyncRunner>.Instance),
            new FullSyncPostSyncPipeline(NullLogger<FullSyncPostSyncPipeline>.Instance),
            new FullSyncCompletionDispatcher(NullLogger<FullSyncCompletionDispatcher>.Instance));

        var msg = new FullSyncMessage
        {
            UserId = "9",
            CompanyId = "company-realm",
            CorrelationId = "corr-1"
        };

        var result = await orchestrator.RunAsync(msg);

        Assert.Single(result.EntityCounts);
        Assert.Equal(5, result.EntityCounts["TestEntity"]);
        Assert.Empty(result.Errors);

        statusRepo.Verify(s => s.SetStatusAsync("company-realm", "Running", null), Times.Once);
        statusRepo.Verify(s => s.SetStatusAsync("company-realm", "Completed", null), Times.Once);

        subscriber.Verify(
            s => s.OnFullSyncCompletedAsync(
                It.Is<FullSyncCompletionContext>(c => c.RealmId == "company-realm" && c.Succeeded),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_StepThrows_RecordsErrorAndSetsPartiallyFailed()
    {
        var statusRepo = new Mock<ISyncStatusRepository>();
        statusRepo.Setup(s => s.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var qboSync = new Mock<IQboSyncStateRepository>();
        qboSync.Setup(q => q.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var warehouse = new Mock<IFinancialWarehouseService>();
        warehouse.Setup(w => w.RebuildForCompanyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var anomaly = new Mock<IAnomalyDetectionService>();
        anomaly.Setup(a => a.DetectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(statusRepo.Object);
        services.AddSingleton(qboSync.Object);
        services.AddSingleton(warehouse.Object);
        services.AddSingleton(anomaly.Object);
        services.AddScoped<SyncContext>();

        await using var provider = services.BuildServiceProvider();

        var step = new Mock<IFullSyncEntitySyncStep>();
        step.Setup(s => s.EntityName).Returns("BadEntity");
        step.Setup(s => s.EntityTypeForSyncState).Returns("BadEntity");
        step.Setup(s => s.ExecuteAsync(It.IsAny<IServiceProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sync failed"));

        var orchestrator = new FullSyncOrchestrator(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<FullSyncOrchestrator>.Instance,
            new[] { step.Object },
            Array.Empty<IFullSyncCompletedSubscriber>(),
            new FullSyncCompanyStatusLifecycle(NullLogger<FullSyncCompanyStatusLifecycle>.Instance),
            new FullSyncEntitySyncRunner(NullLogger<FullSyncEntitySyncRunner>.Instance),
            new FullSyncPostSyncPipeline(NullLogger<FullSyncPostSyncPipeline>.Instance),
            new FullSyncCompletionDispatcher(NullLogger<FullSyncCompletionDispatcher>.Instance));

        var msg = new FullSyncMessage { UserId = "1", CompanyId = "r1", CorrelationId = "c" };
        var result = await orchestrator.RunAsync(msg);

        Assert.NotEmpty(result.Errors);
        statusRepo.Verify(s => s.SetStatusAsync("r1", "PartiallyFailed", It.Is<string>(e => e.Contains("BadEntity", StringComparison.Ordinal))), Times.Once);
    }
}
