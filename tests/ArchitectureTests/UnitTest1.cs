using NetArchTest.Rules;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Bills.Handlers;
using QuickBooksAPI.Features.ChartOfAccounts.Handlers;
using QuickBooksAPI.Features.Customers.Handlers;
using QuickBooksAPI.Features.Invoices.Handlers;
using QuickBooksAPI.Features.JournalEntries.Handlers;
using QuickBooksAPI.Features.Products.Handlers;
using QuickBooksAPI.Features.Vendors.Handlers;
using QuickBooksService.Services;

namespace ArchitectureTests;

/// <summary>
/// Dependency rules for AI readiness.
/// Some repository contracts still use DataAccessLayer.Models; migrated ones (e.g. <see cref="IProductRepository"/>) must not.
/// API-facing read/list interfaces should not reference persistence models (tests per interface below).
/// </summary>
public class DependencyRulesTests
{
    private static string FailureMessage(TestResult result)
    {
        if (result.FailingTypeNames == null)
            return "Architecture rule failed.";

        return string.Join(Environment.NewLine, result.FailingTypeNames);
    }

    [Fact]
    public void Services_ShouldNotDependOnControllers()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Services")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Features_ShouldNotDependOn_Controllers()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Features")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Repositories_ShouldNotDependOnControllers()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.DataAccessLayer.Repos")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void QuickBooksService_ShouldNotDependOn_Controllers()
    {
        var result = Types
            .InAssembly(typeof(IQuickBooksAuthService).Assembly)
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNotDependOn_SqlClient()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Application")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Controllers_ShouldNotDependOn_SqlClient()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Controllers_ShouldNotDependOn_DataAccessLayer_Repos()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Controllers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Repos")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Controllers_ShouldNotDependOn_QuickBooksService_Adapters()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Controllers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Services_ShouldNotDependOn_SqlClient()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Services")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Features_ShouldNotDependOn_SqlClient()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Features")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Features_ShouldNotDependOn_DataAccessLayer_Repos()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Features")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Repos")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Integrations_ShouldNotDependOn_Features()
    {
        var result = Types
            .InAssembly(typeof(IAuthService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("QuickBooksAPI.Integrations")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.Features")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IVendorReadService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IVendorReadService).Assembly)
            .That()
            .HaveName("IVendorReadService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void ICustomerReadService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(ICustomerReadService).Assembly)
            .That()
            .HaveName("ICustomerReadService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IProductService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IProductService).Assembly)
            .That()
            .HaveName("IProductService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IInvoiceReadService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IInvoiceReadService).Assembly)
            .That()
            .HaveName("IInvoiceReadService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IBillReadService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IBillReadService).Assembly)
            .That()
            .HaveName("IBillReadService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IInvoiceService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IInvoiceService).Assembly)
            .That()
            .HaveName("IInvoiceService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IBillService_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IBillService).Assembly)
            .That()
            .HaveName("IBillService")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    /// <summary>
    /// Migrated Products use cases delegate to QBO only via <c>Integrations</c> adapters; handlers must not reference <c>QuickBooksService.Services</c> directly.
    /// </summary>
    [Fact]
    public void IChartOfAccountsRepository_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IChartOfAccountsRepository).Assembly)
            .That()
            .HaveName("IChartOfAccountsRepository")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void ChartOfAccountsFeatureHandlers_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(SyncChartOfAccountsHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.ChartOfAccounts.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void IProductRepository_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(IProductRepository).Assembly)
            .That()
            .HaveName("IProductRepository")
            .And()
            .ResideInNamespace("QuickBooksAPI.Application.Interfaces")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void ProductsFeatureHandlers_ShouldNotDependOn_DataAccessLayer_Models()
    {
        var result = Types
            .InAssembly(typeof(SyncProductsHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.Products.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksAPI.DataAccessLayer.Models")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void ProductsFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncProductsHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.Products.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void ChartOfAccountsFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncChartOfAccountsHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.ChartOfAccounts.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void CustomersFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncCustomersHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.Customers.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void VendorsFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncVendorsHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.Vendors.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void BillsFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncBillsHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.Bills.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void InvoicesFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncInvoicesHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.Invoices.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void JournalEntriesFeatureHandlers_ShouldNotDependOn_QuickBooksService_Services()
    {
        var result = Types
            .InAssembly(typeof(SyncJournalEntriesHandler).Assembly)
            .That()
            .ResideInNamespace("QuickBooksAPI.Features.JournalEntries.Handlers")
            .ShouldNot()
            .HaveDependencyOn("QuickBooksService.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }
}
