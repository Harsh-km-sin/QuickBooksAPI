using NetArchTest.Rules;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksService.Services;

namespace ArchitectureTests;

/// <summary>
/// Dependency rules for AI readiness.
/// Repository contracts (<c>I*Repository</c> in Application.Interfaces) may depend on DataAccessLayer.Models.
/// API-facing read/list interfaces should not, once migrated (tests per interface below).
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
}
