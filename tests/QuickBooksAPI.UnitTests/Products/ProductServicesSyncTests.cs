using Moq;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.Integrations.Abstractions;
using QuickBooksAPI.Services;
using QuickBooksService.Services;

namespace QuickBooksAPI.UnitTests.Products;

public sealed class ProductServicesSyncTests
{
    [Fact]
    public async Task GetProductsAsync_UsesGateway_FirstSyncEmptyResult_UpdatesWatermark()
    {
        var ctx = new Mock<IRequestContext>();
        ctx.Setup(c => c.UserId).Returns("1");
        ctx.Setup(c => c.RealmId).Returns("realm");

        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.RefreshTokenIfExpiredAsync(1, "realm"))
            .ReturnsAsync(new QuickBooksToken { AccessToken = "tok" });

        var gateway = new Mock<IProductAccountingSyncGateway>();
        gateway
            .Setup(g => g.FetchProductsPageAsync("tok", "realm", 1, 1000, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"QueryResponse":{}}""");

        var qboSync = new Mock<IQboSyncStateRepository>();
        qboSync.Setup(s => s.GetLastUpdatedAfterAsync(1, "realm", QboEntityType.Products.ToString()))
            .ReturnsAsync((DateTime?)null);
        qboSync.Setup(s => s.UpdateLastUpdatedAfterAsync(1, "realm", QboEntityType.Products.ToString(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var qb = new Mock<IQuickBooksProductService>();
        var repo = new Mock<IProductRepository>();
        var tokenRepo = new Mock<ITokenRepository>();

        var sut = new ProductServices(
            ctx.Object,
            tokenRepo.Object,
            qb.Object,
            gateway.Object,
            repo.Object,
            qboSync.Object,
            auth.Object);

        var result = await sut.GetProductsAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.Data);
        gateway.Verify(
            g => g.FetchProductsPageAsync("tok", "realm", 1, 1000, null, It.IsAny<CancellationToken>()),
            Times.Once);
        qboSync.Verify(
            s => s.UpdateLastUpdatedAfterAsync(1, "realm", QboEntityType.Products.ToString(), It.IsAny<DateTime>()),
            Times.Once);
    }
}
