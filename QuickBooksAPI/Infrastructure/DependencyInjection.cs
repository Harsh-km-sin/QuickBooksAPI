using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.DataAccessLayer.Sql;

namespace QuickBooksAPI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "DefaultConnection connection string is missing or empty.");

            services.AddSqlDataAccess();

            services.AddSingleton<IFinancialWarehouseRepository>(sp =>
                new FinancialWarehouseRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            // Repositories — SQL access via ISqlConnectionFactory (Phase 2)
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

            services.AddScoped<IQboSyncStateRepository>(sp =>
                new QboSyncStateRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<ISyncStatusRepository>(sp =>
                new SyncStatusRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<IAnomalyEventRepository>(sp =>
                new AnomalyEventRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<IKpiSnapshotRepository>(sp =>
                new KpiSnapshotRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<IForecastScenarioRepository>(sp =>
                new ForecastScenarioRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<IForecastResultRepository>(sp =>
                new ForecastResultRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<ICloseIssueRepository>(sp =>
                new CloseIssueRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<IDimEntityRepository>(sp =>
                new DimEntityRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            services.AddScoped<IConsolidatedPnlRepository>(sp =>
                new ConsolidatedPnlRepository(sp.GetRequiredService<ISqlConnectionFactory>()));

            return services;
        }
    }
}
