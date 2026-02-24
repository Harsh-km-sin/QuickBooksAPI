using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Services;
using QuickBooksService.Services;
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

        // ICurrentUser for worker context
        services.AddScoped<SyncCurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<SyncCurrentUser>());

        // Repositories
        services.AddScoped<ITokenRepository>(_ => new TokenRepository(connectionString));
        services.AddScoped<IAppUserRepository>(_ => new AppUserRepository(connectionString));
        services.AddScoped<ICompanyRepository>(_ => new CompanyRepository(connectionString));
        services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(connectionString));
        services.AddScoped<IVendorRepository>(_ => new VendorRepository(connectionString));
        services.AddScoped<IProductRepository>(_ => new ProductRepository(connectionString));
        services.AddScoped<IChartOfAccountsRepository>(_ => new ChartOfAccountsRepository(connectionString));
        services.AddScoped<IInvoiceRepository>(_ => new InvoiceRepository(connectionString));
        services.AddScoped<IBillRepository>(_ => new BillRepository(connectionString));
        services.AddScoped<IJournalEntryRepository>(_ => new JournalEntryRepository(connectionString));
        services.AddScoped<IQboSyncStateRepository>(_ => new QboSyncStateRepository(connectionString));
        services.AddScoped<ISyncStatusRepository>(_ => new SyncStatusRepository(connectionString));

        // QuickBooks HTTP services
        services.AddHttpClient();
        services.AddScoped<IQuickBooksAuthService, QuickBooksAuthService>();
        services.AddScoped<IQuickBooksCustomerService, QuickBooksCustomerService>();
        services.AddScoped<IQuickBooksVendorService, QuickBooksVendorService>();
        services.AddScoped<IQuickBooksProductService, QuickBooksProductService>();
        services.AddScoped<IQuickBooksChartOfAccountsService, QuickBooksChartOfAccountsService>();
        services.AddScoped<IQuickBooksInvoiceService, QuickBooksInvoiceService>();
        services.AddScoped<IQuickBooksBillService, QuickBooksBillService>();
        services.AddScoped<IQuickBooksJournalEntryService, QuickBooksJournalEntryService>();

        // Application services
        services.AddScoped<IAuthService, AuthServices>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IProductService, ProductServices>();
        services.AddScoped<IChartOfAccountsService, ChartOfAccountsServices>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
    })
    .Build();

host.Run();
