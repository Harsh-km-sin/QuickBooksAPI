using Microsoft.Extensions.DependencyInjection;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.JournalEntries;
using QuickBooksAPI.Features.JournalEntries.Handlers;

namespace QuickBooksAPI.Infrastructure;

public static class JournalEntriesFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddJournalEntriesFeature(this IServiceCollection services)
    {
        services.AddScoped<ListJournalEntriesHandler>();
        services.AddScoped<SyncJournalEntriesHandler>();
        services.AddScoped<IJournalEntryService, JournalEntryServiceMigrationFacade>();
        return services;
    }
}
