using QuickBooksAPI.DataAccessLayer.Repos;

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

            // Repositories
            services.AddScoped<ITokenRepository>(
                _ => new TokenRepository(connectionString));

            services.AddScoped<ICompanyRepository>(
                _ => new CompanyRepository(connectionString));

            services.AddScoped<IAppUserRepository>(
                _ => new AppUserRepository(connectionString));

            services.AddScoped<IChartOfAccountsRepository>(
                _ => new ChartOfAccountsRepository(connectionString));

            services.AddScoped<IProductRepository>(
                _ => new ProductRepository(connectionString));

            services.AddScoped<ICustomerRepository>(
                _ => new CustomerRepository(connectionString));

            services.AddScoped<IVendorRepository>(
                _ => new VendorRepository(connectionString));

            services.AddScoped<IJournalEntryRepository>(
                _ => new JournalEntryRepository(connectionString));

            services.AddScoped<IInvoiceRepository>(
                _ => new InvoiceRepository(connectionString));

            services.AddScoped<IBillRepository>(
                _ => new BillRepository(connectionString));

            services.AddScoped<IQboSyncStateRepository>(
                _ => new QboSyncStateRepository(connectionString));

            return services;
        }
    }
}
