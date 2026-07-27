using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddCoreRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITokenRepository>(sp =>
            new TokenRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<ICompanyRepository>(sp =>
            new CompanyRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IAppUserRepository>(sp =>
            new AppUserRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IChartOfAccountsRepository>(sp =>
            new ChartOfAccountsRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IProductRepository>(sp =>
            new ProductRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<ICustomerRepository>(sp =>
            new CustomerRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IVendorRepository>(sp =>
            new VendorRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IJournalEntryRepository>(sp =>
            new JournalEntryRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IInvoiceRepository>(sp =>
            new InvoiceRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IBillRepository>(sp =>
            new BillRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IReportRepository>(sp =>
            new ReportRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<IQboSyncStateRepository>(sp =>
            new QboSyncStateRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<ITermRepository>(sp =>
            new TermRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

        services.AddScoped<ISyncStatusRepository>(sp =>
            new SyncStatusRepository(sp.GetRequiredService<ISqlConnectionFactory>()));
    }
}
