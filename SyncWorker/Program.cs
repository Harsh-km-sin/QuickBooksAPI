using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure;
using QuickBooksAPI.Services;
using QuickBooksShared;
using SyncWorker;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["DefaultConnection"]
            ?? context.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DefaultConnection is missing.");

        // Typed options for DI consumers (Phase 1) - shared binding to prevent host drift
        services.AddQuickBooksTypedOptions(context.Configuration, validateOnStart: false);

        services.AddScoped<SyncContext>();
        services.AddScoped<ISyncContext>(sp => sp.GetRequiredService<SyncContext>());
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<SyncContext>());

        services.AddInfrastructure(context.Configuration);

        services.AddHttpClient();
        services.AddQuickBooksAuthAndEntityApplicationServices();
        services.AddScoped<IFullSyncOrchestrator, FullSyncOrchestrator>();
        services.AddScoped<IFullSyncCompletedSubscriber, LoggingFullSyncCompletedSubscriber>();
    })
    .Build();

host.Run();
