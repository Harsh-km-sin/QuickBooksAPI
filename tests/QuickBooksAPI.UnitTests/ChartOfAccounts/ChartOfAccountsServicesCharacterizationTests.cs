using Moq;
using QuickBooksAPI.Application.Dtos;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.ChartOfAccounts.Handlers;

namespace QuickBooksAPI.UnitTests.ChartOfAccounts;

/// <summary>
/// Locks list behavior for <see cref="ListChartOfAccountsHandler"/>.
/// </summary>
public sealed class ChartOfAccountsServicesCharacterizationTests
{
    [Fact]
    public async Task ListChartOfAccountsAsync_WithoutUserContext_ReturnsFail()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns((string?)null);
        ctx.Setup(c => c.RealmId).Returns("realm");

        var sut = new ListChartOfAccountsHandler(
            ctx.Object,
            Mock.Of<IChartOfAccountsRepository>());

        var result = await sut.HandleListAsync();

        Assert.False(result.Success);
        Assert.Contains("sign in", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListChartOfAccountsAsync_ReturnsRowsFromRepository()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns("7");
        ctx.Setup(c => c.RealmId).Returns("realm-x");

        var account = new ChartOfAccountsItemDto { Id = 1, Name = "Cash", UserId = 7, RealmId = "realm-x" };

        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetAllByUserAndRealmAsync(7, "realm-x"))
            .ReturnsAsync(new List<ChartOfAccountsItemDto> { account });

        var sut = new ListChartOfAccountsHandler(ctx.Object, repo.Object);

        var result = await sut.HandleListAsync();

        Assert.True(result.Success);
        var list = result.Data!.ToList();
        Assert.Single(list);
        Assert.Equal("Cash", list[0].Name);
    }
}
