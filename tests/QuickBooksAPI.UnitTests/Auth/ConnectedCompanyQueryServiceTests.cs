using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;
using QuickBooksAPI.Services.Auth;

namespace QuickBooksAPI.UnitTests.Auth;

public class ConnectedCompanyQueryServiceTests
{
    [Fact]
    public async Task GetConnectedCompaniesAsync_maps_rows()
    {
        var rows = new List<Company>
        {
            new()
            {
                Id = 1,
                UserId = 3,
                QboRealmId = "a",
                CompanyName = "Co A",
                IsQboConnected = true,
                ConnectedAtUtc = DateTimeOffset.UtcNow
            }
        };

        var repo = new Mock<ICompanyRepository>();
        repo.Setup(x => x.GetConnectedCompaniesByUserIdAsync(3)).ReturnsAsync(rows);

        var sut = new ConnectedCompanyQueryService(repo.Object, NullLogger<ConnectedCompanyQueryService>.Instance);

        var result = await sut.GetConnectedCompaniesAsync(3);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var list = result.Data!.ToList();
        Assert.Single(list);
        Assert.Equal("a", list[0].QboRealmId);
        Assert.Equal("Co A", list[0].CompanyName);
    }

    [Fact]
    public async Task GetConnectedCompaniesAsync_repo_exception_fails()
    {
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(x => x.GetConnectedCompaniesByUserIdAsync(1)).ThrowsAsync(new InvalidOperationException("db"));

        var sut = new ConnectedCompanyQueryService(repo.Object, NullLogger<ConnectedCompanyQueryService>.Instance);

        var result = await sut.GetConnectedCompaniesAsync(1);

        Assert.False(result.Success);
    }
}
